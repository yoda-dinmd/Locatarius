using Locatarius.Domain.Enums;
using Locatarius.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Locatarius.Infrastructure.Residents;

public sealed record ResidentDto(
    Guid Id, string FirstName, string LastName, string Email,
    Guid ApartmentId, string Apartment, string Role, bool IsActive);

public sealed record ApartmentOptionDto(Guid Id, string Apartment, string Building);

public sealed class ResidentAdministrationService(LocatariusDbContext dbContext)
{
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
