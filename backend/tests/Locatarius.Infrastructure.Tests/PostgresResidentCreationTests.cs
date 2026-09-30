using Locatarius.Domain.Entities;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure.Persistence;
using Locatarius.Infrastructure.Residents;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Locatarius.Infrastructure.Tests;

public sealed class PostgresResidentCreationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:18-alpine").Build();
    public async Task InitializeAsync()
    {
        await database.StartAsync();
        await using var db = Context();
        await db.Database.MigrateAsync();
    }
    public Task DisposeAsync() => database.DisposeAsync().AsTask();
    private LocatariusDbContext Context() => new(new DbContextOptionsBuilder<LocatariusDbContext>()
        .UseNpgsql(database.GetConnectionString()).Options);

    [Fact]
    public async Task CredentialWriteFailureRollsBackUserAndRoleTogether()
    {
        await using var db = Context();
        var admin = new User
        {
            UserId = Guid.NewGuid(), FirstName = "Test", LastName = "Admin",
            Credential = new UserCredential { Email = "admin@example.test", PasswordHash = "test-only" },
            Role = new UserRole { Role = UserRoleType.Admin }
        };
        var apartment = new Apartment
        {
            ApartmentId = Guid.NewGuid(), ApartmentNumber = "3A",
            Building = new Building
            {
                BuildingId = Guid.NewGuid(), Admin = admin, BuildingNumber = "A",
                Address = new Address { AddressId = Guid.NewGuid(), Locality = "Chișinău", District = "Centru", Street = "Test" }
            }
        };
        db.Apartments.Add(apartment);
        await db.SaveChangesAsync();
        // Fail the credential insert after the dependent user row has been inserted.
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE user_credentials ADD CONSTRAINT test_reject_email CHECK (email <> 'reject@example.test')");
        var service = new ResidentAdministrationService(db, new PasswordHasher());
        await Assert.ThrowsAsync<DbUpdateException>(() => service.CreateResidentAsync(admin.UserId,
            new("Ana", "Popescu", "reject@example.test", apartment.ApartmentId, "Start password 123!"), default));
        await using var observer = Context();
        Assert.Equal(1, await observer.Users.CountAsync());
        Assert.Equal(1, await observer.UserCredentials.CountAsync());
        Assert.Equal(1, await observer.UserRoles.CountAsync());
    }
}
