using Locatarius.Domain.Entities;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Locatarius.Infrastructure.Auth;

public enum LoginResultStatus
{
    Success,
    InvalidCredentials
}

public sealed record LoginOutcome(
    LoginResultStatus Status,
    string? NextStep,
    string? SessionToken,
    SessionType? SessionType);

public sealed class SessionLookup
{
    public bool IsValid { get; }
    public Session? Session { get; }

    private SessionLookup(bool isValid, Session? session)
    {
        IsValid = isValid;
        Session = session;
    }

    public static SessionLookup Invalid { get; } = new(false, null);
    public static SessionLookup Valid(Session session) => new(true, session);
}

public sealed class AuthenticationService(
    LocatariusDbContext dbContext,
    PasswordHasher passwordHasher)
{
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan FullSessionIdleTimeout = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan FullSessionAbsoluteTimeout = TimeSpan.FromHours(8);
    private static readonly TimeSpan RestrictedSessionAbsoluteTimeout = TimeSpan.FromMinutes(5);
    private const int MaxFailedAttempts = 5;
    private const int MaxConcurrencyRetries = 5;

    // Computed once so unknown-email/locked-account paths still pay a
    // comparable hashing cost to a real verification attempt.
    private static readonly string DummyPasswordHash =
        new PasswordHasher().HashPassword(Guid.NewGuid().ToString("N"));

    public async Task<LoginOutcome> LoginAsync(
        string canonicalEmail,
        string password,
        CancellationToken cancellationToken)
    {
        var credential = await dbContext.UserCredentials
            .Include(c => c.User)
            .ThenInclude(u => u!.Role)
            .SingleOrDefaultAsync(c => c.Email == canonicalEmail, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (credential is null)
        {
            passwordHasher.VerifyPassword(DummyPasswordHash, password);
            return new LoginOutcome(LoginResultStatus.InvalidCredentials, null, null, null);
        }

        if (credential.LockedUntil is { } lockedUntil && lockedUntil > now)
        {
            passwordHasher.VerifyPassword(DummyPasswordHash, password);
            return new LoginOutcome(LoginResultStatus.InvalidCredentials, null, null, null);
        }

        if (credential.LockedUntil is { } expiredLock && expiredLock <= now)
        {
            await ResetFailedAttemptsAsync(credential, cancellationToken);
        }

        var passwordValid = passwordHasher.VerifyPassword(credential.PasswordHash, password);

        if (!passwordValid)
        {
            await RecordFailedAttemptAsync(credential, now, cancellationToken);
            return new LoginOutcome(LoginResultStatus.InvalidCredentials, null, null, null);
        }

        await ResetFailedAttemptsAsync(credential, cancellationToken);

        var sessionType = credential.MustChangePassword
            ? SessionType.PasswordChange
            : SessionType.Full;

        var absoluteLifetime = sessionType == SessionType.Full
            ? FullSessionAbsoluteTimeout
            : RestrictedSessionAbsoluteTimeout;

        var rawToken = SessionTokenGenerator.GenerateToken();
        var tokenHash = SessionTokenGenerator.HashToken(rawToken);

        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            UserId = credential.UserId,
            TokenHash = tokenHash,
            SessionType = sessionType,
            CreatedAt = now,
            ExpiresAt = now.Add(absoluteLifetime),
            LastSeenAt = sessionType == SessionType.Full ? now : null,
            RevokedAt = null
        };

        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        var nextStep = sessionType == SessionType.Full ? "app" : "change_password";

        return new LoginOutcome(LoginResultStatus.Success, nextStep, rawToken, sessionType);
    }

    public async Task<SessionLookup> ResolveSessionAsync(
        string? rawToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(rawToken))
        {
            return SessionLookup.Invalid;
        }

        var tokenHash = SessionTokenGenerator.HashToken(rawToken);

        var session = await dbContext.Sessions
            .SingleOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken);

        if (session is null || session.RevokedAt is not null)
        {
            return SessionLookup.Invalid;
        }

        var now = DateTimeOffset.UtcNow;

        if (session.ExpiresAt <= now)
        {
            return SessionLookup.Invalid;
        }

        if (session.SessionType == SessionType.Full
            && session.LastSeenAt is { } lastSeen
            && now - lastSeen > FullSessionIdleTimeout)
        {
            return SessionLookup.Invalid;
        }

        return SessionLookup.Valid(session);
    }

    public async Task LogoutAsync(Session session, CancellationToken cancellationToken)
    {
        dbContext.Sessions.Remove(session);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Increments the failure counter and applies the lockout window once the
    /// threshold is hit. FailedLoginAttempts/LockedUntil are configured as
    /// concurrency tokens, so a concurrent writer that already touched this
    /// row causes SaveChangesAsync to throw DbUpdateConcurrencyException
    /// instead of silently losing an increment; we reload the current DB
    /// values and reapply on top of them.
    /// </summary>
    private async Task RecordFailedAttemptAsync(
        UserCredential credential, DateTimeOffset now, CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            credential.FailedLoginAttempts += 1;

            if (credential.FailedLoginAttempts >= MaxFailedAttempts)
            {
                credential.LockedUntil = now.Add(LockoutDuration);
            }

            try
            {
                await dbContext.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await ReloadFromDatabaseAsync(ex, ct);
            }
        }

        throw new InvalidOperationException(
            "Could not persist failed login attempt after retrying on concurrent writes.");
    }

    private async Task ResetFailedAttemptsAsync(
        UserCredential credential, CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            credential.FailedLoginAttempts = 0;
            credential.LockedUntil = null;

            try
            {
                await dbContext.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await ReloadFromDatabaseAsync(ex, ct);
            }
        }

        throw new InvalidOperationException(
            "Could not reset failed login attempts after retrying on concurrent writes.");
    }

    private static async Task ReloadFromDatabaseAsync(
        DbUpdateConcurrencyException ex, CancellationToken ct)
    {
        foreach (var entry in ex.Entries)
        {
            await entry.ReloadAsync(ct);
        }
    }
}