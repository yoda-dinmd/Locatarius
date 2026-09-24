using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Locatarius.Domain.Entities;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure.Auth;
using Locatarius.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Locatarius.Infrastructure.Tests;

public sealed class ResidentAdministrationHttpTests
{
    [Fact]
    public async Task DirectoryReturnsOnlyOwnedResidentsIncludingInactiveWithSafeOrderedFields()
    {
        await using var factory = new DirectoryFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await SeedDirectory(factory, client);

        var response = await client.GetAsync("/api/residents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var residents = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(residents);
        Assert.Equal(new[] { "Ana", "Ioana", "Mihai" },
            residents.Select(r => r.GetProperty("firstName").GetString()).ToArray());
        Assert.Equal(new[] { "3A", "12", "3A" },
            residents.Select(r => r.GetProperty("apartment").GetString()).ToArray());
        Assert.Equal(new[] { true, false, true },
            residents.Select(r => r.GetProperty("isActive").GetBoolean()).ToArray());
        Assert.All(residents, resident =>
        {
            Assert.Equal("Resident", resident.GetProperty("role").GetString());
            Assert.NotEqual(Guid.Empty, resident.GetProperty("id").GetGuid());
            Assert.NotEqual(Guid.Empty, resident.GetProperty("apartmentId").GetGuid());
            Assert.EndsWith("@a.example.test", resident.GetProperty("email").GetString());
            Assert.Equal(new[] { "apartment", "apartmentId", "email", "firstName", "id", "isActive", "lastName", "role" },
                resident.EnumerateObject().Select(p => p.Name).Order().ToArray());
        });
    }

    [Fact]
    public async Task ApartmentOptionsIncludeOnlyOwnedBuildingsInBuildingThenApartmentOrder()
    {
        await using var factory = new DirectoryFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await SeedDirectory(factory, client);

        var response = await client.GetAsync("/api/apartments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var apartments = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(apartments);
        Assert.Equal(new[] { "12", "3A", "1" }, apartments.Select(a => a.GetProperty("apartment").GetString()).ToArray());
        Assert.Equal(new[] { "Building A1", "Building A1", "Building A2" }, apartments.Select(a => a.GetProperty("building").GetString()).ToArray());
        Assert.All(apartments, apartment =>
        {
            Assert.NotEqual(Guid.Empty, apartment.GetProperty("id").GetGuid());
            Assert.Equal(new[] { "apartment", "building", "id" }, apartment.EnumerateObject().Select(p => p.Name).Order().ToArray());
        });
    }

    [Theory]
    [InlineData("/api/residents")]
    [InlineData("/api/apartments")]
    public async Task AdminWithoutBuildingsGetsEmptyDirectory(string path)
    {
        await using var factory = new DirectoryFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await SeedSession(factory, client, "admin");

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("missing", HttpStatusCode.Unauthorized)]
    [InlineData("unknown", HttpStatusCode.Unauthorized)]
    [InlineData("expired", HttpStatusCode.Unauthorized)]
    [InlineData("revoked", HttpStatusCode.Unauthorized)]
    [InlineData("inactive", HttpStatusCode.Unauthorized)]
    [InlineData("resident", HttpStatusCode.Forbidden)]
    [InlineData("restricted", HttpStatusCode.Forbidden)]
    public async Task BothDirectoryEndpointsRequireActiveFullAdminSession(string condition, HttpStatusCode expected)
    {
        await using var factory = new DirectoryFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await SeedSession(factory, client, condition);

        foreach (var path in new[] { "/api/residents", "/api/apartments" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(expected, response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
            var error = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error");
            Assert.Equal(expected == HttpStatusCode.Unauthorized ? "UNAUTHENTICATED" : "FORBIDDEN", error.GetProperty("code").GetString());
        }
    }

    private static async Task SeedDirectory(DirectoryFactory factory, HttpClient client)
    {
        var adminId = await SeedSession(factory, client, "admin");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LocatariusDbContext>();
        var otherAdmin = NewUser("Other", "Admin", "admin@b.example.test", UserRoleType.Admin);
        var buildingA1 = NewBuilding(adminId, "Building A1");
        var buildingA2 = NewBuilding(adminId, "Building A2");
        var buildingB = NewBuilding(otherAdmin.UserId, "Building B");
        var apartment3A = NewApartment(buildingA1, "3A");
        var apartment12 = NewApartment(buildingA1, "12");
        var apartmentB = NewApartment(buildingB, "3A");
        db.Users.Add(otherAdmin);
        db.Apartments.AddRange(apartment3A, apartment12, apartmentB, NewApartment(buildingA2, "1"));
        var ana = NewUser("Ana", "Popescu", "ana@a.example.test");
        ana.Apartment = apartment3A;
        var ioana = NewUser("Ioana", "Popescu", "ioana@a.example.test");
        ioana.Apartment = apartment12;
        ioana.IsActive = false;
        var mihai = NewUser("Mihai", "Rusu", "mihai@a.example.test");
        mihai.Apartment = apartment3A;
        var otherResident = NewUser("Bogdan", "AdminB", "bogdan@b.example.test");
        otherResident.Apartment = apartmentB;
        var unassigned = NewUser("Unassigned", "Resident", "unassigned@a.example.test");
        var apartmentAdmin = NewUser("Apartment", "Admin", "apartment-admin@a.example.test", UserRoleType.Admin);
        apartmentAdmin.Apartment = apartment3A;
        db.Users.AddRange(mihai, ioana, ana, otherResident, unassigned, apartmentAdmin);
        await db.SaveChangesAsync();
    }

    private static async Task<Guid> SeedSession(DirectoryFactory factory, HttpClient client, string condition)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LocatariusDbContext>();
        var user = NewUser("Test", "Admin", "admin@example.test",
            condition == "resident" ? UserRoleType.Resident : UserRoleType.Admin);
        user.IsActive = condition != "inactive";
        user.Credential!.MustChangePassword = condition == "restricted";
        db.Users.Add(user);
        if (condition is not ("missing" or "unknown"))
        {
            var now = DateTimeOffset.UtcNow;
            db.Sessions.Add(new Session
            {
                SessionId = Guid.NewGuid(), UserId = user.UserId,
                TokenHash = SessionTokenGenerator.HashToken("directory-test-session"),
                SessionType = condition == "restricted" ? SessionType.PasswordChange : SessionType.Full,
                CreatedAt = now.AddMinutes(-2), LastSeenAt = now.AddMinutes(-1),
                ExpiresAt = condition == "expired" ? now.AddSeconds(-1) : now.AddHours(1),
                RevokedAt = condition == "revoked" ? now : null
            });
        }
        if (condition != "missing") client.DefaultRequestHeaders.Add("Cookie", "__Host-locatarius=directory-test-session");
        await db.SaveChangesAsync();
        return user.UserId;
    }

    private static User NewUser(string firstName, string lastName, string email, UserRoleType role = UserRoleType.Resident)
    {
        var id = Guid.NewGuid();
        return new User
        {
            UserId = id, FirstName = firstName, LastName = lastName,
            Credential = new UserCredential { UserId = id, Email = email, PasswordHash = "not-returned-secret", MustChangePassword = false },
            Role = new UserRole { UserId = id, Role = role }
        };
    }

    private static Building NewBuilding(Guid adminId, string number) => new()
    {
        BuildingId = Guid.NewGuid(), AdminId = adminId, BuildingNumber = number,
        Address = new Address { AddressId = Guid.NewGuid(), Locality = "Chișinău", District = "Centru", Street = "Ștefan cel Mare" }
    };

    private static Apartment NewApartment(Building building, string number) => new()
    {
        ApartmentId = Guid.NewGuid(), Building = building, BuildingId = building.BuildingId, ApartmentNumber = number
    };

    private sealed class DirectoryFactory : WebApplicationFactory<Program>
    {
        private readonly string database = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RUN_MIGRATIONS"] = "false", ["EXIT_AFTER_MIGRATIONS"] = "false", ["Proxy:TrustedProxy"] = ""
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<LocatariusDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<LocatariusDbContext>>();
                services.AddDbContext<LocatariusDbContext>(options => options.UseInMemoryDatabase(database));
            });
        }
    }
}
