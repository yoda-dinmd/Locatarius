using Locatarius.Domain.Enums;
using Locatarius.Infrastructure;
using Locatarius.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Locatarius.Infrastructure.Tests;

public sealed class DemoDataSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesExpectedDemoDataset()
    {
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

        Assert.Equal(
            1,
            await context.UserRoles.CountAsync(
                role => role.Role == UserRoleType.Admin));

        Assert.Equal(
            7,
            await context.UserRoles.CountAsync(
                role => role.Role == UserRoleType.Resident));
    }

    [Fact]
    public async Task SeedAsync_CreatesIntegerStyleApartmentNumbers()
    {
        await using var context = CreateContext();

        await CreateAdminSeeder(context).SeedAsync();
        await CreateDemoSeeder(context).SeedAsync();

        var apartmentNumbers = await context.Apartments
            .OrderBy(apartment => apartment.ApartmentNumber)
            .Select(apartment => apartment.ApartmentNumber)
            .ToListAsync();

        Assert.Equal(
            ["101", "102", "201", "202", "301", "302", "303"],
            apartmentNumbers);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        await using var context = CreateContext();

        await CreateAdminSeeder(context).SeedAsync();

        var seeder = CreateDemoSeeder(context);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

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
    public async Task SeedAsync_DoesNothingWhenDisabled()
    {
        await using var context = CreateContext();

        await CreateAdminSeeder(context).SeedAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedDemoData:Enabled"] = "false",
                ["SeedAdmin:Email"] = "admin@locatarius.md"
            })
            .Build();

        var seeder = new DemoDataSeeder(
            context,
            configuration,
            new PasswordHasher());

        await seeder.SeedAsync();

        Assert.Equal(1, await context.Users.CountAsync());
        Assert.Empty(await context.Apartments.ToListAsync());
    }

    [Fact]
    public async Task SeedAsync_RequiresDemoPasswordWhenEnabled()
    {
        await using var context = CreateContext();

        await CreateAdminSeeder(context).SeedAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedDemoData:Enabled"] = "true",
                ["SeedAdmin:Email"] = "admin@locatarius.md"
            })
            .Build();

        var seeder = new DemoDataSeeder(
            context,
            configuration,
            new PasswordHasher());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => seeder.SeedAsync());

        Assert.Contains("SeedDemoData:Password", exception.Message);
    }

    private static LocatariusDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocatariusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LocatariusDbContext(options);
    }

    private static DatabaseSeeder CreateAdminSeeder(
        LocatariusDbContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedAdmin:Email"] = "admin@locatarius.md",
                ["SeedAdmin:Password"] = "Admin123!",
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
                ["SeedDemoData:Enabled"] = "true",
                ["SeedDemoData:Password"] = "Demo123!",
                ["SeedAdmin:Email"] = "admin@locatarius.md"
            })
            .Build();

        return new DemoDataSeeder(
            context,
            configuration,
            new PasswordHasher());
    }
}