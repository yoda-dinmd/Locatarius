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
    PasswordHasher passwordHasher,
    TimeProvider? timeProvider = null)
{
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan FullSessionIdleTimeout = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan FullSessionAbsoluteTimeout = TimeSpan.FromHours(8);
    private static readonly TimeSpan RestrictedSessionAbsoluteTimeout = TimeSpan.FromMinutes(5);
    private const int MaxFailedAttempts = 5;
    private TimeProvider Clock => timeProvider ?? TimeProvider.System;

    // Computed once so unknown-email/locked-account paths still pay a
    // comparable hashing cost to a real verification attempt.
    private static readonly string DummyPasswordHash =
        new PasswordHasher().HashPassword(Guid.NewGuid().ToString("N"));

    public async Task<LoginOutcome> LoginAsync(
        string canonicalEmail,
        string password,
        CancellationToken cancellationToken)
    {
        // All auth-sensitive writes must lock user, then credential, then sessions.
        // InMemory is used by sequential unit tests only; concurrency tests use PostgreSQL.
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Users.FromSqlInterpolated($"""
                SELECT u.* FROM users AS u
                JOIN user_credentials AS c ON c.user_id = u.user_id
                WHERE c.email = {canonicalEmail} FOR UPDATE OF u
                """).AsNoTracking().ToListAsync(cancellationToken);
        }

        var credential = dbContext.Database.IsRelational()
            ? await dbContext.UserCredentials.FromSqlInterpolated($"""
                SELECT * FROM user_credentials WHERE email = {canonicalEmail} FOR UPDATE
                """).SingleOrDefaultAsync(cancellationToken)
            : await dbContext.UserCredentials.SingleOrDefaultAsync(
                c => c.Email == canonicalEmail, cancellationToken);
        // Never make a security decision using an entity tracked before the lock.
        if (credential is not null && dbContext.Database.IsRelational())
            await dbContext.Entry(credential).ReloadAsync(cancellationToken);

        var now = Clock.GetUtcNow();

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
            credential.FailedLoginAttempts = 0;
            credential.LockedUntil = null;
        }

        var passwordValid = passwordHasher.VerifyPassword(credential.PasswordHash, password);

        if (!passwordValid)
        {
            credential.FailedLoginAttempts++;
            if (credential.FailedLoginAttempts >= MaxFailedAttempts)
                credential.LockedUntil = now.Add(LockoutDuration);
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new LoginOutcome(LoginResultStatus.InvalidCredentials, null, null, null);
        }

        credential.FailedLoginAttempts = 0;
        credential.LockedUntil = null;

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

        if (transaction is not null) await transaction.CommitAsync(cancellationToken);

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

        var now = Clock.GetUtcNow();

        if (session.ExpiresAt <= now)
        {
            return SessionLookup.Invalid;
        }

        if (session.SessionType == SessionType.Full
            && (session.LastSeenAt is not { } lastSeen
                || now - lastSeen >= FullSessionIdleTimeout))
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
    /// Call only after an accepted authenticated request. Does not resurrect
    /// revoked/expired sessions or slide restricted sessions. #83's protected
    /// request pipeline must call this after its authentication/authorization checks.
    /// </summary>
    public async Task<bool> RecordAcceptedActivityAsync(Guid sessionId, CancellationToken ct)
    {
        var now = Clock.GetUtcNow();
        var cutoff = now.Subtract(FullSessionIdleTimeout);
        var active = dbContext.Sessions.Where(s => s.SessionId == sessionId
            && s.SessionType == SessionType.Full && s.RevokedAt == null
            && s.ExpiresAt > now && s.LastSeenAt > cutoff);
        if (dbContext.Database.IsRelational())
        {
            // A concurrent later request must not have its timestamp moved backwards.
            var updated = await active.Where(s => s.LastSeenAt <= now)
                .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.LastSeenAt, now), ct);
            return updated != 0 || await active.AnyAsync(ct);
        }
        var session = await active.SingleOrDefaultAsync(ct);
        if (session is null) return false;
        if (session.LastSeenAt < now) session.LastSeenAt = now;
        await dbContext.SaveChangesAsync(ct);
        return true;
    }
}
