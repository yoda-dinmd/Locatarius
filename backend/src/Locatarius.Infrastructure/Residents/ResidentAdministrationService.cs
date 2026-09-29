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
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_user_credentials_email" })
        {
            dbContext.Entry(user.Role).State = EntityState.Detached;
            dbContext.Entry(user.Credential).State = EntityState.Detached;
            dbContext.Entry(user).State = EntityState.Detached;
            return new(CreateResidentStatus.DuplicateEmail);
        }
        return new(CreateResidentStatus.Created, new ResidentDto(id, user.FirstName, user.LastName,
            user.Credential.Email, apartment.ApartmentId, apartment.ApartmentNumber, "Resident", true));
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
}
