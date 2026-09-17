using Locatarius.Domain.Entities;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure;
using Locatarius.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Locatarius.Infrastructure.Tests;

public sealed class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesAdminWithNormalizedEmailAndChangePasswordFlag()
    {
        await using var context = CreateContext();
        await CreateSeeder(
            context,
            email: " ADMIN@LOCATARIUS.MD ",
            password: "Admin123!").SeedAsync();

        var user = await context.Users.SingleAsync();
        var credential = await context.UserCredentials.SingleAsync();
        var role = await context.UserRoles.SingleAsync();

        Assert.Equal("admin@locatarius.md", credential.Email);
        Assert.Equal(UserRoleType.Admin, role.Role);
        Assert.Equal("Admin", user.FirstName);
        Assert.Equal("User", user.LastName);
        Assert.True(credential.MustChangePassword);
        Assert.True(
            new PasswordHasher().VerifyPassword(
                credential.PasswordHash,
                "Admin123!"));
    }

    [Fact]
    public async Task SeedAsync_DoesNotCreateDuplicates()
    {
        await using var context = CreateContext();
        var seeder = CreateSeeder(context, password: "Admin123!");

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

        await CreateSeeder(context, password: "Admin123!").SeedAsync();

        var originalHash = await context.UserCredentials
            .Select(credential => credential.PasswordHash)
            .SingleAsync();

        await CreateSeeder(
            context,
            password: "DifferentPassword123!").SeedAsync();

        var currentHash = await context.UserCredentials
            .Select(credential => credential.PasswordHash)
            .SingleAsync();

        Assert.Equal(originalHash, currentHash);
    }

    [Fact]
    public async Task SeedAsync_RejectsExistingNonAdminAccount()
    {
        await using var context = CreateContext();

        var user = new User
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

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSeeder(
                context,
                password: "Admin123!").SeedAsync());

        Assert.Contains("non-admin", exception.Message);
        Assert.DoesNotContain("Admin123!", exception.Message);

        Assert.Equal(
            UserRoleType.Resident,
            await context.UserRoles
                .Select(role => role.Role)
                .SingleAsync());
    }

    [Fact]
    public async Task SeedAsync_RejectsAccountWithoutRole()
    {
        await using var context = CreateContext();

        var user = new User
        {
            UserId = Guid.NewGuid(),
            FirstName = "Incomplete",
            LastName = "User",
            Credential = new UserCredential
            {
                Email = "admin@locatarius.md",
                PasswordHash = "existing-hash"
            }
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSeeder(context).SeedAsync());

        Assert.Contains("no role", exception.Message);
    }

    [Fact]
    public async Task SeedAsync_RejectsAccountWithEmptyPasswordHash()
    {
        await using var context = CreateContext();

        var user = new User
        {
            UserId = Guid.NewGuid(),
            FirstName = "Incomplete",
            LastName = "User",
            Credential = new UserCredential
            {
                Email = "admin@locatarius.md",
                PasswordHash = string.Empty
            },
            Role = new UserRole
            {
                Role = UserRoleType.Admin
            }
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSeeder(context).SeedAsync());

        Assert.Contains("incomplete credentials", exception.Message);
    }

    [Theory]
    [InlineData(null, "Admin123!", "Admin", "User")]
    [InlineData("admin@locatarius.md", null, "Admin", "User")]
    [InlineData("admin@locatarius.md", "Admin123!", null, "User")]
    [InlineData("admin@locatarius.md", "Admin123!", "Admin", null)]
    public async Task SeedAsync_RejectsMissingConfiguration(
        string? email,
        string? password,
        string? firstName,
        string? lastName)
    {
        await using var context = CreateContext();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSeeder(
                context,
                email,
                password,
                firstName,
                lastName).SeedAsync());

        Assert.DoesNotContain("Admin123!", exception.Message);
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
        string? email = "ADMIN@LOCATARIUS.MD",
        string? password = "Admin123!",
        string? firstName = "Admin",
        string? lastName = "User")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedAdmin:Email"] = email,
                ["SeedAdmin:Password"] = password,
                ["SeedAdmin:FirstName"] = firstName,
                ["SeedAdmin:LastName"] = lastName
            })
            .Build();

        return new DatabaseSeeder(
            context,
            configuration,
            new PasswordHasher());
    }
}