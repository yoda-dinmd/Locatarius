using Locatarius.Domain.Entities;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure.Auth;
using Locatarius.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Locatarius.Infrastructure.Tests;

public sealed class AuthenticationServiceTests
{
    private const string Password = "CorrectPassword123!";

    [Fact]
    public async Task LoginAsync_ValidPermanentPassword_ReturnsFullSession()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context, mustChangePassword: false);
        var service = CreateService(context);

        var outcome = await service.LoginAsync("resident@example.com", Password, default);

        Assert.Equal(LoginResultStatus.Success, outcome.Status);
        Assert.Equal("app", outcome.NextStep);
        Assert.Equal(SessionType.Full, outcome.SessionType);
        Assert.NotNull(outcome.SessionToken);

        var session = await context.Sessions.SingleAsync();
        Assert.Equal(userId, session.UserId);
        Assert.Equal(SessionType.Full, session.SessionType);
        Assert.NotNull(session.LastSeenAt);
    }

    [Fact]
    public async Task LoginAsync_TemporaryPassword_ReturnsRestrictedSession()
    {
        await using var context = CreateContext();
        await SeedUserAsync(context, mustChangePassword: true);
        var service = CreateService(context);

        var outcome = await service.LoginAsync("resident@example.com", Password, default);

        Assert.Equal(LoginResultStatus.Success, outcome.Status);
        Assert.Equal("change_password", outcome.NextStep);
        Assert.Equal(SessionType.PasswordChange, outcome.SessionType);

        var session = await context.Sessions.SingleAsync();
        Assert.Equal(SessionType.PasswordChange, session.SessionType);
        Assert.Null(session.LastSeenAt);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsInvalidCredentialsAndNoSession()
    {
        await using var context = CreateContext();
        await SeedUserAsync(context, mustChangePassword: false);
        var service = CreateService(context);

        var outcome = await service.LoginAsync("resident@example.com", "WrongPassword!", default);

        Assert.Equal(LoginResultStatus.InvalidCredentials, outcome.Status);
        Assert.Null(outcome.SessionToken);
        Assert.Empty(context.Sessions);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsSameInvalidCredentialsOutcome()
    {
        await using var context = CreateContext();
        await SeedUserAsync(context, mustChangePassword: false);
        var service = CreateService(context);

        var outcome = await service.LoginAsync("nobody@example.com", Password, default);

        Assert.Equal(LoginResultStatus.InvalidCredentials, outcome.Status);
        Assert.Empty(context.Sessions);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsInvalidCredentialsAndNoSession()
    {
        await using var context = CreateContext();
        await SeedUserAsync(context, mustChangePassword: false);
        var user = await context.Users.Include(x => x.Credential)
            .SingleAsync(x => x.Credential!.Email == "resident@example.com");
        user.IsActive = false;
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var outcome = await service.LoginAsync(
            "resident@example.com", Password, CancellationToken.None);

        Assert.Equal(LoginResultStatus.InvalidCredentials, outcome.Status);
        Assert.Null(outcome.SessionToken);
        Assert.Empty(await context.Sessions.Where(x => x.UserId == user.UserId).ToListAsync());
    }

    [Fact]
    public async Task LoginAsync_FifthFailedAttempt_LocksAccountForFifteenMinutes()
    {
        await using var context = CreateContext();
        await SeedUserAsync(context, mustChangePassword: false);
        var service = CreateService(context);

        for (var i = 0; i < 5; i++)
        {
            await service.LoginAsync("resident@example.com", "WrongPassword!", default);
        }

        // Correct password during the lockout window still fails.
        var outcome = await service.LoginAsync("resident@example.com", Password, default);
        Assert.Equal(LoginResultStatus.InvalidCredentials, outcome.Status);
        Assert.Empty(context.Sessions);

        var credential = await context.UserCredentials.SingleAsync();
        Assert.Equal(5, credential.FailedLoginAttempts);
        Assert.True(credential.LockedUntil > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_AfterLockoutExpires_CorrectPasswordSucceedsAndResetsCounter()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context, mustChangePassword: false);
        var service = CreateService(context);

        var credential = await context.UserCredentials.SingleAsync();
        credential.FailedLoginAttempts = 5;
        credential.LockedUntil = DateTimeOffset.UtcNow.AddSeconds(-1); // already expired
        await context.SaveChangesAsync();

        var outcome = await service.LoginAsync("resident@example.com", Password, default);

        Assert.Equal(LoginResultStatus.Success, outcome.Status);

        var updated = await context.UserCredentials.SingleAsync();
        Assert.Equal(0, updated.FailedLoginAttempts);
        Assert.Null(updated.LockedUntil);
    }

    [Fact]
    public async Task LoginAsync_SuccessfulLogin_ResetsPreviousFailedAttempts()
    {
        await using var context = CreateContext();
        await SeedUserAsync(context, mustChangePassword: false);
        var service = CreateService(context);

        await service.LoginAsync("resident@example.com", "Wrong1!", default);
        await service.LoginAsync("resident@example.com", "Wrong2!", default);
        await service.LoginAsync("resident@example.com", Password, default);

        var credential = await context.UserCredentials.SingleAsync();
        Assert.Equal(0, credential.FailedLoginAttempts);
        Assert.Null(credential.LockedUntil);
    }

    [Fact]
    public async Task ResolveSessionAsync_UnknownToken_ReturnsInvalid()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var lookup = await service.ResolveSessionAsync("not-a-real-token", default);

        Assert.False(lookup.IsValid);
    }

    [Fact]
    public async Task ResolveSessionAsync_ExpiredSession_ReturnsInvalid()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context, mustChangePassword: false);
        var (rawToken, _) = await SeedSessionAsync(
            context, userId, SessionType.Full,
            expiresAt: DateTimeOffset.UtcNow.AddSeconds(-1),
            lastSeenAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        var service = CreateService(context);
        var lookup = await service.ResolveSessionAsync(rawToken, default);

        Assert.False(lookup.IsValid);
    }

    [Fact]
    public async Task ResolveSessionAsync_IdleFullSessionPastThirtyMinutes_ReturnsInvalid()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context, mustChangePassword: false);
        var (rawToken, _) = await SeedSessionAsync(
            context, userId, SessionType.Full,
            expiresAt: DateTimeOffset.UtcNow.AddHours(7),
            lastSeenAt: DateTimeOffset.UtcNow.AddMinutes(-31));

        var service = CreateService(context);
        var lookup = await service.ResolveSessionAsync(rawToken, default);

        Assert.False(lookup.IsValid);
    }

    [Fact]
    public async Task ResolveSessionAsync_ValidFullSession_ReturnsValid()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context, mustChangePassword: false);
        var (rawToken, sessionId) = await SeedSessionAsync(
            context, userId, SessionType.Full,
            expiresAt: DateTimeOffset.UtcNow.AddHours(7),
            lastSeenAt: DateTimeOffset.UtcNow.AddMinutes(-5));

        var service = CreateService(context);
        var lookup = await service.ResolveSessionAsync(rawToken, default);

        Assert.True(lookup.IsValid);
        Assert.Equal(sessionId, lookup.Session!.SessionId);
    }

    [Fact]
    public async Task LogoutAsync_RemovesSession_ReplayOfSameTokenIsInvalid()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context, mustChangePassword: false);
        var (rawToken, _) = await SeedSessionAsync(
            context, userId, SessionType.Full,
            expiresAt: DateTimeOffset.UtcNow.AddHours(7),
            lastSeenAt: DateTimeOffset.UtcNow);

        var service = CreateService(context);
        var lookup = await service.ResolveSessionAsync(rawToken, default);
        Assert.True(lookup.IsValid);

        await service.LogoutAsync(lookup.Session!, default);

        var replay = await service.ResolveSessionAsync(rawToken, default);
        Assert.False(replay.IsValid);
        Assert.Empty(context.Sessions);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReplacesHashAndAllSessions_AndRejectsReplay()
    {
        await using var context = CreateContext();
        var userId = await SeedUserAsync(context, mustChangePassword: true);
        var service = CreateService(context);
        var login = await service.LoginAsync("resident@example.com", Password, default);
        var lookup = await service.ResolveSessionAsync(login.SessionToken, default);
        await service.LoginAsync("resident@example.com", Password, default);
        var credential = await context.UserCredentials.SingleAsync();

        var outcome = await service.ChangePasswordAsync(lookup.Session!, "ResidentPrivatePassword456!", default);

        Assert.Equal(LoginResultStatus.Success, outcome.Status);
        Assert.Equal(SessionType.Full, outcome.SessionType);
        Assert.Equal("app", outcome.NextStep);
        Assert.False(credential.MustChangePassword);
        Assert.NotNull(credential.PasswordChangedAt);
        Assert.True(new PasswordHasher().VerifyPassword(credential.PasswordHash, "ResidentPrivatePassword456!"));
        Assert.False(new PasswordHasher().VerifyPassword(credential.PasswordHash, Password));
        var fullSession = await context.Sessions.SingleAsync();
        Assert.Equal(userId, fullSession.UserId);
        Assert.Equal(SessionType.Full, fullSession.SessionType);
        Assert.Equal(SessionTokenGenerator.HashToken(outcome.SessionToken!), fullSession.TokenHash);
        Assert.NotNull(fullSession.LastSeenAt);
        Assert.False((await service.ResolveSessionAsync(login.SessionToken, default)).IsValid);
        Assert.Equal(LoginResultStatus.InvalidCredentials,
            (await service.ChangePasswordAsync(lookup.Session!, "AnotherPrivatePassword789!", default)).Status);
    }

    [Theory]
    [InlineData("full")]
    [InlineData("expired")]
    [InlineData("revoked")]
    [InlineData("inactive")]
    [InlineData("already_changed")]
    [InlineData("missing")]
    public async Task ChangePasswordAsync_InvalidState_DoesNotChangePassword(string state)
    {
        await using var context = CreateContext();
        await SeedUserAsync(context, mustChangePassword: true);
        var service = CreateService(context);
        var login = await service.LoginAsync("resident@example.com", Password, default);
        var session = (await service.ResolveSessionAsync(login.SessionToken, default)).Session!;
        var credential = await context.UserCredentials.SingleAsync();
        var originalHash = credential.PasswordHash;
        switch (state)
        {
            case "full": session.SessionType = SessionType.Full; break;
            case "expired": session.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1); break;
            case "revoked": session.RevokedAt = DateTimeOffset.UtcNow; break;
            case "inactive": (await context.Users.SingleAsync()).IsActive = false; break;
            case "already_changed": credential.MustChangePassword = false; break;
            case "missing": context.Sessions.Remove(session); break;
        }
        await context.SaveChangesAsync();

        var outcome = await service.ChangePasswordAsync(session, "ResidentPrivatePassword456!", default);

        Assert.Equal(LoginResultStatus.InvalidCredentials, outcome.Status);
        Assert.Null(outcome.SessionToken);
        Assert.Equal(originalHash, credential.PasswordHash);
        Assert.Null(credential.PasswordChangedAt);
    }

    private static LocatariusDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocatariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LocatariusDbContext(options);
    }

    private static AuthenticationService CreateService(LocatariusDbContext context)
        => new(context, new PasswordHasher());

    private static async Task<Guid> SeedUserAsync(
        LocatariusDbContext context, bool mustChangePassword)
    {
        var userId = Guid.NewGuid();

        context.Users.Add(new User
        {
            UserId = userId,
            FirstName = "Resident",
            LastName = "User",
            Credential = new UserCredential
            {
                UserId = userId,
                Email = "resident@example.com",
                PasswordHash = new PasswordHasher().HashPassword(Password),
                MustChangePassword = mustChangePassword
            },
            Role = new UserRole
            {
                UserId = userId,
                Role = UserRoleType.Resident
            }
        });

        await context.SaveChangesAsync();
        return userId;
    }

    private static async Task<(string RawToken, Guid SessionId)> SeedSessionAsync(
        LocatariusDbContext context,
        Guid userId,
        SessionType sessionType,
        DateTimeOffset expiresAt,
        DateTimeOffset? lastSeenAt)
    {
        var rawToken = SessionTokenGenerator.GenerateToken();
        var sessionId = Guid.NewGuid();

        context.Sessions.Add(new Session
        {
            SessionId = sessionId,
            UserId = userId,
            TokenHash = SessionTokenGenerator.HashToken(rawToken),
            SessionType = sessionType,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            ExpiresAt = expiresAt,
            LastSeenAt = lastSeenAt,
            RevokedAt = null
        });

        await context.SaveChangesAsync();
        return (rawToken, sessionId);
    }
}
