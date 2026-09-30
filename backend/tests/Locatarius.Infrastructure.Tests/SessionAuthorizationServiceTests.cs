using Locatarius.Domain.Entities;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure.Auth;
using Locatarius.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Locatarius.Infrastructure.Tests;

public sealed class SessionAuthorizationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(SessionType.Full, UserRoleType.Admin, null, false, SessionAuthorizationStatus.Authorized)]
    [InlineData(SessionType.PasswordChange, UserRoleType.Resident, null, false, SessionAuthorizationStatus.Authorized)]
    [InlineData(SessionType.Full, UserRoleType.Admin, SessionType.Full, true, SessionAuthorizationStatus.Authorized)]
    [InlineData(SessionType.Full, UserRoleType.Resident, SessionType.Full, true, SessionAuthorizationStatus.Forbidden)]
    [InlineData(SessionType.PasswordChange, UserRoleType.Admin, SessionType.Full, true, SessionAuthorizationStatus.WrongSessionType)]
    [InlineData(SessionType.Full, UserRoleType.Admin, SessionType.PasswordChange, false, SessionAuthorizationStatus.WrongSessionType)]
    [InlineData((SessionType)99, UserRoleType.Admin, null, false, SessionAuthorizationStatus.Unauthenticated)]
    public async Task EnforcesSessionAndDatabaseRoleBeforeRecordingActivity(
        SessionType actualType, UserRoleType role, SessionType? requiredType,
        bool requireAdmin, SessionAuthorizationStatus expected)
    {
        await using var db = CreateContext();
        var userId = await Seed(db, actualType, role);
        var result = await CreateService(db).AuthorizeAsync("test-session", requiredType, requireAdmin, default);

        Assert.Equal(expected, result.Status);
        if (expected == SessionAuthorizationStatus.Authorized)
        {
            Assert.Equal(userId, result.User!.UserId);
            Assert.NotNull(result.User.Credential);
            Assert.Equal(role, result.User.Role!.Role);
            Assert.NotNull(result.Session);
        }
        var session = await db.Sessions.SingleAsync();
        Assert.Equal(actualType != SessionType.Full ? (DateTimeOffset?)null :
            expected == SessionAuthorizationStatus.Authorized ? Now : Now.AddMinutes(-5), session.LastSeenAt);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("missing-user")]
    [InlineData("missing-role")]
    [InlineData("missing-credential")]
    [InlineData("unknown-role")]
    public async Task IncompleteOrInactiveIdentityIsRejectedWithoutExtendingSession(string condition)
    {
        await using var db = CreateContext();
        await Seed(db, SessionType.Full, UserRoleType.Resident);
        var user = await db.Users.SingleAsync();
        if (condition == "inactive") user.IsActive = false;
        if (condition == "missing-user") (await db.Sessions.SingleAsync()).UserId = Guid.NewGuid();
        if (condition == "missing-role") db.UserRoles.Remove(user.Role!);
        if (condition == "missing-credential") db.UserCredentials.Remove(user.Credential!);
        if (condition == "unknown-role") user.Role!.Role = (UserRoleType)99;
        await db.SaveChangesAsync();

        var result = await CreateService(db).AuthorizeAsync("test-session", null, false, default);

        Assert.Equal(SessionAuthorizationStatus.Unauthenticated, result.Status);
        Assert.Null(result.User);
        Assert.Null(result.Session);
        Assert.Equal(Now.AddMinutes(-5), (await db.Sessions.SingleAsync()).LastSeenAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("unknown")]
    public async Task MissingOrUnknownTokenCannotAuthorize(string? token)
    {
        await using var db = CreateContext();
        await Seed(db, SessionType.Full, UserRoleType.Admin);
        var result = await CreateService(db).AuthorizeAsync(token, null, false, default);
        Assert.Equal(SessionAuthorizationStatus.Unauthenticated, result.Status);
        Assert.Equal(Now.AddMinutes(-5), (await db.Sessions.SingleAsync()).LastSeenAt);
    }

    private static LocatariusDbContext CreateContext() => new(new DbContextOptionsBuilder<LocatariusDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static SessionAuthorizationService CreateService(LocatariusDbContext db)
        => new(db, new AuthenticationService(db, new PasswordHasher(), new FixedClock()));

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static async Task<Guid> Seed(LocatariusDbContext db, SessionType sessionType, UserRoleType role)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            UserId = userId, FirstName = "Test", LastName = "User",
            Credential = new UserCredential { UserId = userId, Email = "identity@example.test", PasswordHash = "unused" },
            Role = new UserRole { UserId = userId, Role = role }
        });
        db.Sessions.Add(new Session
        {
            SessionId = Guid.NewGuid(), UserId = userId, TokenHash = SessionTokenGenerator.HashToken("test-session"),
            SessionType = sessionType, CreatedAt = Now.AddMinutes(-5), ExpiresAt = Now.AddHours(1),
            LastSeenAt = sessionType == SessionType.Full ? Now.AddMinutes(-5) : null
        });
        await db.SaveChangesAsync();
        return userId;
    }
}
