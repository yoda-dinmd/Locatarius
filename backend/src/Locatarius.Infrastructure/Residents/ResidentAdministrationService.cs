using Locatarius.Domain.Enums;
using Locatarius.Domain.Entities;
using Locatarius.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Locatarius.Infrastructure.Residents;

public sealed record ResidentDto(
    Guid Id, string FirstName, string LastName, string Email,
    Guid ApartmentId, string Apartment, string Role, bool IsActive);

public sealed record ApartmentOptionDto(Guid Id, string Apartment, string Building);

public sealed record CreateResidentCommand(
    string FirstName, string LastName, string CanonicalEmail, Guid ApartmentId, string TemporaryPassword)
{
    public override string ToString() => nameof(CreateResidentCommand);
}

public enum CreateResidentStatus { Created, DuplicateEmail, ApartmentNotFound }
public sealed record CreateResidentResult(CreateResidentStatus Status, ResidentDto? Resident = null);

public sealed record UpdateResidentCommand(
    string FirstName, string LastName, string CanonicalEmail, Guid ApartmentId, bool IsActive);

public enum UpdateResidentStatus { Updated, ResidentNotFound, ApartmentNotFound, DuplicateEmail }
public sealed record UpdateResidentResult(UpdateResidentStatus Status, ResidentDto? Resident = null);

public enum DeactivateResidentStatus { Deactivated, ResidentNotFound }

public sealed class ResidentAdministrationService(LocatariusDbContext dbContext, PasswordHasher passwordHasher)
{
    public async Task<CreateResidentResult> CreateResidentAsync(
        Guid adminId, CreateResidentCommand command, CancellationToken cancellationToken)
    {
        var apartment = await dbContext.Apartments.AsNoTracking().SingleOrDefaultAsync(
            apartment => apartment.ApartmentId == command.ApartmentId && apartment.Building.AdminId == adminId,
            cancellationToken);
        if (apartment is null) return new(CreateResidentStatus.ApartmentNotFound);
        if (await dbContext.UserCredentials.AnyAsync(c => c.Email == command.CanonicalEmail, cancellationToken))
            return new(CreateResidentStatus.DuplicateEmail);
        var id = Guid.NewGuid();
        var user = new User
        {
            UserId = id, FirstName = command.FirstName, LastName = command.LastName,
            ApartmentId = command.ApartmentId, IsActive = true,
            Role = new UserRole { UserId = id, Role = UserRoleType.Resident },
            Credential = new UserCredential
            {
                UserId = id, Email = command.CanonicalEmail,
                PasswordHash = passwordHasher.HashPassword(command.TemporaryPassword), MustChangePassword = true
            }
        };
        dbContext.Users.Add(user);
        try
        {
            // One SaveChanges call atomically persists the whole graph on relational providers.
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsEmailUniqueViolation(exception))
        {
            dbContext.Entry(user.Role).State = EntityState.Detached;
            dbContext.Entry(user.Credential).State = EntityState.Detached;
            dbContext.Entry(user).State = EntityState.Detached;
            return new(CreateResidentStatus.DuplicateEmail);
        }
        return new(CreateResidentStatus.Created, new ResidentDto(id, user.FirstName, user.LastName,
            user.Credential.Email, apartment.ApartmentId, apartment.ApartmentNumber, "Resident", true));
    }

    public async Task<UpdateResidentResult> UpdateResidentAsync(
        Guid adminId, Guid residentId, UpdateResidentCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var user = await LockScopedResidentAsync(adminId, residentId, includeCredential: true, cancellationToken);
        if (user is null || user.Credential is null)
            return new(UpdateResidentStatus.ResidentNotFound);

        var apartment = await dbContext.Apartments.AsNoTracking().SingleOrDefaultAsync(
            a => a.ApartmentId == command.ApartmentId && a.Building.AdminId == adminId,
            cancellationToken);
        if (apartment is null)
            return new(UpdateResidentStatus.ApartmentNotFound);

        if (await dbContext.UserCredentials.AnyAsync(
            c => c.Email == command.CanonicalEmail && c.UserId != residentId, cancellationToken))
            return new(UpdateResidentStatus.DuplicateEmail);

        var credential = user.Credential;
        user.FirstName = command.FirstName;
        user.LastName = command.LastName;
        user.ApartmentId = apartment.ApartmentId;
        user.IsActive = command.IsActive;
        credential.Email = command.CanonicalEmail;

        List<Session>? removedSessions = null;
        if (!command.IsActive && !dbContext.Database.IsRelational())
        {
            removedSessions = await dbContext.Sessions
                .Where(s => s.UserId == residentId)
                .ToListAsync(cancellationToken);
            dbContext.Sessions.RemoveRange(removedSessions);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsEmailUniqueViolation(exception))
        {
            dbContext.Entry(credential).State = EntityState.Detached;
            dbContext.Entry(user).State = EntityState.Detached;
            if (removedSessions is not null)
            {
                foreach (var session in removedSessions)
                    dbContext.Entry(session).State = EntityState.Detached;
            }
            return new(UpdateResidentStatus.DuplicateEmail);
        }

        if (!command.IsActive && dbContext.Database.IsRelational())
        {
            // Lock order: users -> user_credentials -> sessions.
            await dbContext.Sessions.Where(s => s.UserId == residentId).ExecuteDeleteAsync(cancellationToken);
        }

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return new(UpdateResidentStatus.Updated, new ResidentDto(
            user.UserId, user.FirstName, user.LastName, credential.Email,
            apartment.ApartmentId, apartment.ApartmentNumber, "Resident", user.IsActive));
    }

    public async Task<DeactivateResidentStatus> DeactivateResidentAsync(
        Guid adminId, Guid residentId, CancellationToken cancellationToken)
    {
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var user = await LockScopedResidentAsync(adminId, residentId, includeCredential: false, cancellationToken);
        if (user is null)
            return DeactivateResidentStatus.ResidentNotFound;

        user.IsActive = false;

        if (dbContext.Database.IsRelational())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            // Lock order: users -> sessions.
            await dbContext.Sessions.Where(s => s.UserId == residentId).ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            var sessions = await dbContext.Sessions
                .Where(s => s.UserId == residentId)
                .ToListAsync(cancellationToken);
            dbContext.Sessions.RemoveRange(sessions);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return DeactivateResidentStatus.Deactivated;
    }

    public async Task<List<ResidentDto>> ListResidentsAsync(Guid adminId, CancellationToken cancellationToken)
        => await dbContext.Users.AsNoTracking()
            .Where(user => user.Role!.Role == UserRoleType.Resident
                && user.Apartment != null && user.Apartment.Building.AdminId == adminId)
            .OrderBy(user => user.LastName).ThenBy(user => user.FirstName)
            .Select(user => new ResidentDto(
                user.UserId, user.FirstName, user.LastName, user.Credential!.Email,
                user.ApartmentId!.Value, user.Apartment!.ApartmentNumber, "Resident", user.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<List<ApartmentOptionDto>> ListApartmentsAsync(Guid adminId, CancellationToken cancellationToken)
        => await dbContext.Apartments.AsNoTracking()
            .Where(apartment => apartment.Building.AdminId == adminId)
            .OrderBy(apartment => apartment.Building.BuildingNumber).ThenBy(apartment => apartment.ApartmentNumber)
            .Select(apartment => new ApartmentOptionDto(
                apartment.ApartmentId, apartment.ApartmentNumber, apartment.Building.BuildingNumber))
            .ToListAsync(cancellationToken);

    private async Task<User?> LockScopedResidentAsync(
        Guid adminId, Guid residentId, bool includeCredential, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsRelational())
        {
            var residentRole = (int)UserRoleType.Resident;
            var user = await dbContext.Users.FromSqlInterpolated($"""
                SELECT u.* FROM users AS u
                JOIN user_role AS r ON r.user_id = u.user_id
                JOIN apartments AS a ON a.apartment_id = u.apartment_id
                JOIN buildings AS b ON b.building_id = a.building_id
                WHERE u.user_id = {residentId}
                  AND r.role = {residentRole}
                  AND b.admin_id = {adminId}
                FOR UPDATE OF u
                """).SingleOrDefaultAsync(cancellationToken);
            if (user is null) return null;

            await dbContext.Entry(user).ReloadAsync(cancellationToken);
            var stillInScope = await dbContext.Users.AsNoTracking().AnyAsync(
                u => u.UserId == residentId
                    && u.Role!.Role == UserRoleType.Resident
                    && u.Apartment != null
                    && u.Apartment.Building.AdminId == adminId,
                cancellationToken);
            if (!stillInScope) return null;

            if (includeCredential)
            {
                var credential = await dbContext.UserCredentials.FromSqlInterpolated($"""
                    SELECT * FROM user_credentials WHERE user_id = {residentId} FOR UPDATE
                    """).SingleOrDefaultAsync(cancellationToken);
                if (credential is null) return null;
                await dbContext.Entry(credential).ReloadAsync(cancellationToken);
                user.Credential = credential;
            }

            return user;
        }

        IQueryable<User> query = dbContext.Users;
        if (includeCredential) query = query.Include(u => u.Credential);
        return await query.SingleOrDefaultAsync(
            user => user.UserId == residentId
                && user.Role!.Role == UserRoleType.Resident
                && user.Apartment != null
                && user.Apartment.Building.AdminId == adminId,
            cancellationToken);
    }

    private static bool IsEmailUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_user_credentials_email"
        };
}
