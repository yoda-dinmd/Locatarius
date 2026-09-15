using Locatarius.Domain.Enums;
using Locatarius.Infrastructure;
using Locatarius.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Locatarius.Infrastructure.Tests;

public sealed class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesAdminUser()
    {
        await using var context = CreateContext();
        var seeder = CreateSeeder(context, "Admin123!");

        await seeder.SeedAsync();

        var credential = await context.UserCredentials.SingleAsync();
        var role = await context.UserRoles.SingleAsync();
        var user = await context.Users.SingleAsync();

        Assert.Equal("admin@locatarius.local", credential.Email);
        Assert.Equal(UserRoleType.Admin, role.Role);
        Assert.Equal("Admin", user.FirstName);
        Assert.Equal("User", user.LastName);
        Assert.True(
            new PasswordHasher().VerifyPassword(
                credential.PasswordHash,
                "Admin123!"));
    }

    [Fact]
    public async Task SeedAsync_DoesNotCreateDuplicates()
    {
        await using var context = CreateContext();
        var seeder = CreateSeeder(context, "Admin123!");

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        Assert.Equal(1, await context.Users.CountAsync());
        Assert.Equal(1, await context.UserCredentials.CountAsync());
        Assert.Equal(1, await context.UserRoles.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_DoesNotReplaceExistingPassword()
    {
        await using var context = CreateContext();

        await CreateSeeder(context, "Admin123!").SeedAsync();

        var originalHash = await context.UserCredentials
            .Select(credential => credential.PasswordHash)
            .SingleAsync();

        await CreateSeeder(context, "DifferentPassword123!")
            .SeedAsync();

        var currentHash = await context.UserCredentials
            .Select(credential => credential.PasswordHash)
            .SingleAsync();

        Assert.Equal(originalHash, currentHash);
    }

    private static LocatariusDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocatariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LocatariusDbContext(options);
    }

    private static DatabaseSeeder CreateSeeder(
        LocatariusDbContext context,
        string password)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedAdmin:Email"] = "ADMIN@LOCATARIUS.LOCAL",
                ["SeedAdmin:Password"] = password,
                ["SeedAdmin:FirstName"] = "Admin",
                ["SeedAdmin:LastName"] = "User"
            })
            .Build();

        return new DatabaseSeeder(
            context,
            configuration,
            new PasswordHasher());
    }
}