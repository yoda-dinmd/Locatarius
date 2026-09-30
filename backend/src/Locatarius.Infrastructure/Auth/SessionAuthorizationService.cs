using Locatarius.Domain.Entities;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Locatarius.Infrastructure.Auth;

public enum SessionAuthorizationStatus
{
    Authorized,
    Unauthenticated,
    WrongSessionType,
    Forbidden
}

public sealed record SessionAuthorizationResult(
    SessionAuthorizationStatus Status,
    Session? Session,
    User? User);

public sealed class SessionAuthorizationService(
    LocatariusDbContext dbContext,
    AuthenticationService authenticationService)
{
    public async Task<SessionAuthorizationResult> AuthorizeAsync(
        string? rawToken,
        SessionType? requiredType,
        bool requireAdmin,
        CancellationToken cancellationToken)
    {
        var lookup = await authenticationService.ResolveSessionAsync(rawToken, cancellationToken);
        if (!lookup.IsValid)
            return Denied(SessionAuthorizationStatus.Unauthenticated);

        var session = lookup.Session!;
        if (session.SessionType is not (SessionType.Full or SessionType.PasswordChange))
            return Denied(SessionAuthorizationStatus.Unauthenticated);

        var user = await dbContext.Users.AsNoTracking()
            .Include(u => u.Credential).Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.UserId == session.UserId, cancellationToken);
        if (user is null || !user.IsActive || user.Credential is null
            || user.Role?.Role is not (UserRoleType.Admin or UserRoleType.Resident))
            return Denied(SessionAuthorizationStatus.Unauthenticated);

        if (requiredType is not null && session.SessionType != requiredType)
            return Denied(SessionAuthorizationStatus.WrongSessionType);

        if (requireAdmin && user.Role.Role != UserRoleType.Admin)
            return Denied(SessionAuthorizationStatus.Forbidden);

        // Only accepted full sessions slide. A concurrent expiry/revocation
        // detected by the conditional update must still deny the request.
        if (session.SessionType == SessionType.Full
            && !await authenticationService.RecordAcceptedActivityAsync(session.SessionId, cancellationToken))
            return Denied(SessionAuthorizationStatus.Unauthenticated);

        return new(SessionAuthorizationStatus.Authorized, session, user);
    }

    private static SessionAuthorizationResult Denied(SessionAuthorizationStatus status)
        => new(status, null, null);
}
