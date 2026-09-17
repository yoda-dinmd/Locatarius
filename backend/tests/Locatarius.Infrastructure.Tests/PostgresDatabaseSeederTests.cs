using Locatarius.Domain.Entities;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure;
using Locatarius.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Locatarius.Infrastructure.Tests;

public sealed class PostgresDatabaseSeederTests
    : IAsyncLifetime
{
    private readonly PostgreSqlContainer container =
        new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithDatabase("locatarius_test")
            .WithUsername("locatarius_test")
            .WithPassword("locatarius_test_password")
            .Build();

    public async Task InitializeAsync()
    {
        await container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await container.DisposeAsync();
    }

    [Fact]
    public async Task FreshDatabase_AppliesMigrationsAndCreatesOneAdmin()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();

        await CreateAdminSeeder(context).SeedAsync();

        Assert.Equal(1, await context.Users.CountAsync());
        Assert.Equal(1, await context.UserCredentials.CountAsync());
        Assert.Equal(1, await context.UserRoles.CountAsync());

        var credential = await context.UserCredentials.SingleAsync();
        var role = await context.UserRoles.SingleAsync();

        Assert.Equal("admin@locatarius.md", credential.Email);
        Assert.Equal(UserRoleType.Admin, role.Role);
        Assert.True(credential.MustChangePassword);
        Assert.True(
            new PasswordHasher().VerifyPassword(
                credential.PasswordHash,
                "Admin123!"));
    }

    [Fact]
    public async Task RepeatedStartup_DoesNotCreateDuplicatesOrReplacePassword()
    {
        await ResetDatabaseAsync();

        await using var firstContext = CreateContext();

        var seeder = CreateAdminSeeder(firstContext);

        await seeder.SeedAsync();

        var originalHash = await firstContext.UserCredentials
            .Select(credential => credential.PasswordHash)
            .SingleAsync();

        await using var secondContext = CreateContext();

        await CreateAdminSeeder(secondContext, "Different123!")
            .SeedAsync();

        Assert.Equal(1, await secondContext.Users.CountAsync());
        Assert.Equal(1, await secondContext.UserCredentials.CountAsync());
        Assert.Equal(1, await secondContext.UserRoles.CountAsync());

        var currentHash = await secondContext.UserCredentials
            .Select(credential => credential.PasswordHash)
            .SingleAsync();

        Assert.Equal(originalHash, currentHash);
    }

    [Fact]
    public async Task InvalidConfiguration_ReturnsActionableErrorWithoutPassword()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedAdmin:Email"] = "admin@locatarius.md",
                ["SeedAdmin:Password"] = "SuperSecretPassword123!",
                ["SeedAdmin:FirstName"] = "Ion",
                ["SeedAdmin:LastName"] = null
            })
            .Build();

        var seeder = new DatabaseSeeder(
            context,
            configuration,
            new PasswordHasher());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => seeder.SeedAsync());

        Assert.Contains("SeedAdmin:LastName", exception.Message);
        Assert.DoesNotContain(
            "SuperSecretPassword123!",
            exception.Message);
    }

    [Fact]
    public async Task ExistingNonAdminAccount_IsRejected()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();

        var resident = new User
        {
            UserId = Guid.NewGuid(),
            FirstName = "Resident",
            LastName = "User",
            Credential = new UserCredential
            {
                Email = "admin@locatarius.md",
                PasswordHash = new PasswordHasher()
                    .HashPassword("Existing123!"),
                MustChangePassword = false
            },
            Role = new UserRole
            {
                Role = UserRoleType.Resident
            }
        };

        context.Users.Add(resident);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateAdminSeeder(context).SeedAsync());

        Assert.Contains("non-admin", exception.Message);

        Assert.Equal(
            UserRoleType.Resident,
            await context.UserRoles
                .Select(userRole => userRole.Role)
                .SingleAsync());

        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Fact]
    public async Task IncompleteAdminRecord_IsRejected()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();

        var incompleteUser = new User
        {
            UserId = Guid.NewGuid(),
            FirstName = "Incomplete",
            LastName = "Admin",
            Credential = new UserCredential
            {
                Email = "admin@locatarius.md",
                PasswordHash = "existing-hash"
            }
        };

        context.Users.Add(incompleteUser);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateAdminSeeder(context).SeedAsync());

        Assert.Contains("no role", exception.Message);
    }

    [Fact]
    public async Task FreshDatabase_CreatesCompleteDemoDataset()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();

        await CreateAdminSeeder(context).SeedAsync();
        await CreateDemoSeeder(context).SeedAsync();

        Assert.Equal(8, await context.Users.CountAsync());
        Assert.Equal(8, await context.UserCredentials.CountAsync());
        Assert.Equal(8, await context.UserRoles.CountAsync());
        Assert.Equal(8, await context.UserContacts.CountAsync());
        Assert.Equal(2, await context.Addresses.CountAsync());
        Assert.Equal(2, await context.Buildings.CountAsync());
        Assert.Equal(7, await context.Apartments.CountAsync());
        Assert.Equal(3, await context.Issues.CountAsync());
        Assert.Equal(2, await context.IssueAttachments.CountAsync());
        Assert.Equal(1, await context.OtpCodes.CountAsync());
    }

    [Fact]
    public async Task RepeatedStartup_DoesNotDuplicateDemoDataset()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();

        await CreateAdminSeeder(context).SeedAsync();

        var demoSeeder = CreateDemoSeeder(context);

        await demoSeeder.SeedAsync();
        await demoSeeder.SeedAsync();

        Assert.Equal(8, await context.Users.CountAsync());
        Assert.Equal(8, await context.UserCredentials.CountAsync());
        Assert.Equal(8, await context.UserRoles.CountAsync());
        Assert.Equal(8, await context.UserContacts.CountAsync());
        Assert.Equal(7, await context.Apartments.CountAsync());
        Assert.Equal(3, await context.Issues.CountAsync());
        Assert.Equal(2, await context.IssueAttachments.CountAsync());
        Assert.Equal(1, await context.OtpCodes.CountAsync());
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    private LocatariusDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocatariusDbContext>()
            .UseNpgsql(container.GetConnectionString())
            .Options;

        return new LocatariusDbContext(options);
    }

    private static DatabaseSeeder CreateAdminSeeder(
        LocatariusDbContext context,
        string password = "Admin123!")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedAdmin:Email"] = "ADMIN@LOCATARIUS.MD",
                ["SeedAdmin:Password"] = password,
                ["SeedAdmin:FirstName"] = "Ion",
                ["SeedAdmin:LastName"] = "Popescu"
            })
            .Build();

        return new DatabaseSeeder(
            context,
            configuration,
            new PasswordHasher());
    }

    private static DemoDataSeeder CreateDemoSeeder(
        LocatariusDbContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedAdmin:Email"] = "admin@locatarius.md",
                ["SeedDemoData:Enabled"] = "true",
                ["SeedDemoData:Password"] = "Demo123!"
            })
            .Build();

        return new DemoDataSeeder(
            context,
            configuration,
            new PasswordHasher());
    }
}