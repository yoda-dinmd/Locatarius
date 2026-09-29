using Locatarius.Domain.Entities;
using Locatarius.Infrastructure.Persistence;
using Locatarius.Infrastructure.Residents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Locatarius.Infrastructure.Tests;

public sealed class ResidentCreationPersistenceTests
{
    [Theory]
    [InlineData("23505", "IX_user_credentials_email", true)]
    [InlineData("23505", "another_unique_constraint", false)]
    [InlineData("23503", "IX_user_credentials_email", false)]
    public async Task OnlyCanonicalEmailUniqueViolationsBecomeConflicts(string sqlState, string constraint, bool conflict)
    {
        var failure = new SaveFailure(sqlState, constraint);
        var options = new DbContextOptionsBuilder<LocatariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).AddInterceptors(failure).Options;
        await using var db = new LocatariusDbContext(options);
        var adminId = Guid.NewGuid();
        var apartment = new Apartment
        {
            ApartmentId = Guid.NewGuid(), ApartmentNumber = "3A",
            Building = new Building { BuildingId = Guid.NewGuid(), AdminId = adminId }
        };
        db.Apartments.Add(apartment);
        await db.SaveChangesAsync();
        failure.Enabled = true;
        var service = new ResidentAdministrationService(db, new PasswordHasher());
        var command = new CreateResidentCommand("Ana", "Popescu", "ana@example.test", apartment.ApartmentId, "Start password 123!");
        if (conflict)
        {
            var result = await service.CreateResidentAsync(adminId, command, default);
            Assert.Equal(CreateResidentStatus.DuplicateEmail, result.Status);
            Assert.Null(result.Resident);
            Assert.DoesNotContain(db.ChangeTracker.Entries(), entry => entry.State == EntityState.Added);
        }
        else await Assert.ThrowsAsync<DbUpdateException>(() => service.CreateResidentAsync(adminId, command, default));
        await using var observer = new LocatariusDbContext(options);
        Assert.Empty(await observer.Users.ToListAsync());
        Assert.Empty(await observer.UserCredentials.ToListAsync());
        Assert.Empty(await observer.UserRoles.ToListAsync());
    }

    // Simulates a provider write failure after the pre-check, without pretending
    // to verify a real PostgreSQL transaction or database uniqueness constraint.
    private sealed class SaveFailure(string sqlState, string constraint) : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Enabled) throw new DbUpdateException("Provider write failed.",
                new PostgresException("conflict", "ERROR", "ERROR", sqlState, constraintName: constraint));
            return ValueTask.FromResult(result);
        }
    }
}
