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

// Exercises the real HTTP/auth pipeline without requiring Docker. PostgreSQL
// coverage remains in AuthHttpTests and the persistence test suites.
public sealed class AuthIdentityHttpTests
{
    private const string Password = "CorrectTemporaryPassword123!";

    [Theory]
    [InlineData(UserRoleType.Admin, true, "admin")]
    [InlineData(UserRoleType.Resident, false, "resident")]
    public async Task MeReturnsOnlyServerDerivedIdentityAfterCsrfLogin(
        UserRoleType role, bool mustChangePassword, string apiRole)
    {
        await using var factory = new IdentityFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var userId = await SeedUser(factory, role, mustChangePassword);
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "identity@example.test", password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var identity = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(userId, identity.GetProperty("id").GetGuid());
        Assert.Equal("Test User", identity.GetProperty("name").GetString());
        Assert.Equal("identity@example.test", identity.GetProperty("email").GetString());
        Assert.Equal(apiRole, identity.GetProperty("role").GetString());
        Assert.Equal(mustChangePassword, identity.GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal(new[] { "email", "id", "mustChangePassword", "name", "role" },
            identity.EnumerateObject().Select(p => p.Name).Order().ToArray());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("unknown")]
    [InlineData("expired")]
    [InlineData("revoked")]
    [InlineData("inactive")]
    public async Task MeRejectsInvalidAuthentication(string condition)
    {
        await using var factory = new IdentityFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var userId = await SeedUser(factory, UserRoleType.Resident, false);
        if (condition != "missing")
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LocatariusDbContext>();
            var now = DateTimeOffset.UtcNow;
            if (condition == "inactive") (await db.Users.SingleAsync()).IsActive = false;
            if (condition != "unknown") db.Sessions.Add(new Session
            {
                SessionId = Guid.NewGuid(), UserId = userId,
                TokenHash = SessionTokenGenerator.HashToken("test-session"), SessionType = SessionType.Full,
                CreatedAt = now.AddMinutes(-5), LastSeenAt = now.AddMinutes(-1),
                ExpiresAt = condition == "expired" ? now.AddSeconds(-1) : now.AddHours(1),
                RevokedAt = condition == "revoked" ? now : null
            });
            await db.SaveChangesAsync();
            client.DefaultRequestHeaders.Add("Cookie", "__Host-locatarius=test-session");
        }

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error");
        Assert.Equal("UNAUTHENTICATED", error.GetProperty("code").GetString());
    }

    private static async Task<Guid> SeedUser(IdentityFactory factory, UserRoleType role, bool mustChangePassword)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LocatariusDbContext>();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            UserId = userId, FirstName = "Test", LastName = "User",
            Credential = new UserCredential
            {
                UserId = userId, Email = "identity@example.test",
                PasswordHash = new PasswordHasher().HashPassword(Password), MustChangePassword = mustChangePassword
            },
            Role = new UserRole { UserId = userId, Role = role }
        });
        await db.SaveChangesAsync();
        return userId;
    }

    private sealed class IdentityFactory : WebApplicationFactory<Program>
    {
        private readonly string database = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RUN_MIGRATIONS"] = "false", ["EXIT_AFTER_MIGRATIONS"] = "false",
                ["Proxy:TrustedProxy"] = ""
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
