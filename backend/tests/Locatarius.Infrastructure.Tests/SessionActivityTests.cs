using Locatarius.Domain.Entities;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure;
using Locatarius.Infrastructure.Auth;
using Locatarius.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Locatarius.Infrastructure.Tests;

public sealed class SessionActivityTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.Parse("2026-09-18T10:00:00Z");
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task AcceptedActivitySlidesIdleButNotAbsoluteDeadline()
    {
        await using var db = CreateContext();
        var clock = new Clock();
        var token = SessionTokenGenerator.GenerateToken();
        var session = AddSession(db, token, SessionType.Full, clock.Now);
        await db.SaveChangesAsync();
        var service = new AuthenticationService(db, new PasswordHasher(), clock);
        clock.Now = clock.Now.AddMinutes(29);
        Assert.True((await service.ResolveSessionAsync(token, default)).IsValid);
        // Lookup alone must not extend a session for a subsequently denied request.
        Assert.NotEqual(clock.Now, session.LastSeenAt);
        Assert.True(await service.RecordAcceptedActivityAsync(session.SessionId, default));
        clock.Now = clock.Now.AddMinutes(29);
        Assert.True((await service.ResolveSessionAsync(token, default)).IsValid);
        clock.Now = session.ExpiresAt;
        Assert.False((await service.ResolveSessionAsync(token, default)).IsValid);
        Assert.False(await service.RecordAcceptedActivityAsync(session.SessionId, default));
    }

    [Fact]
    public async Task ExactIdleDeadlineAndRevokedOrRestrictedSessionsDoNotSlide()
    {
        await using var db = CreateContext();
        var clock = new Clock();
        var token = SessionTokenGenerator.GenerateToken();
        var session = AddSession(db, token, SessionType.Full, clock.Now);
        await db.SaveChangesAsync();
        var service = new AuthenticationService(db, new PasswordHasher(), clock);
        clock.Now = clock.Now.AddMinutes(30);
        Assert.False((await service.ResolveSessionAsync(token, default)).IsValid);
        Assert.False(await service.RecordAcceptedActivityAsync(session.SessionId, default));
        var restricted = AddSession(db, "restricted", SessionType.PasswordChange, clock.Now);
        var revoked = AddSession(db, "revoked", SessionType.Full, clock.Now);
        revoked.RevokedAt = clock.Now;
        await db.SaveChangesAsync();
        Assert.False(await service.RecordAcceptedActivityAsync(restricted.SessionId, default));
        Assert.False(await service.RecordAcceptedActivityAsync(revoked.SessionId, default));
        db.Sessions.Remove(session);
        await db.SaveChangesAsync();
        Assert.False(await service.RecordAcceptedActivityAsync(session.SessionId, default));
    }

    private static Session AddSession(LocatariusDbContext db, string token, SessionType type, DateTimeOffset now)
    {
        var session = new Session { SessionId = Guid.NewGuid(), UserId = Guid.NewGuid(),
            TokenHash = SessionTokenGenerator.HashToken(token), SessionType = type,
            CreatedAt = now, ExpiresAt = now.AddHours(8), LastSeenAt = now };
        db.Sessions.Add(session);
        return session;
    }
    private static LocatariusDbContext CreateContext() => new(
        new DbContextOptionsBuilder<LocatariusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
