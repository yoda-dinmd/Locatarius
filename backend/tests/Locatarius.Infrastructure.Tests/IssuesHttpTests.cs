using System.Net;
using System.Net.Http.Json;
using System.Text;
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

public sealed class IssuesHttpTests
{
    private static readonly string[] IssueFields =
    [
        "attachments", "buildingId", "buildingNumber", "createdAt", "description", "issueId", "priority",
        "reportedBy", "reporter", "resolvedAt", "status", "title", "updatedAt"
    ];

    // ---------- Resident flows ----------

    [Fact]
    public async Task ResidentCreatesIssueInOwnBuildingWithServerAssignedFields()
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, world.ResidentA, csrf: true);

        var response = await client.PostAsJsonAsync("/api/issues",
            new { title = "  Leaking pipe in hallway ", description = "Water drips from the ceiling\nnear the lift.", priority = 3 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var dto = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = dto.GetProperty("issueId").GetGuid();
        Assert.Equal($"/api/issues/{id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal(IssueFields, dto.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal("Leaking pipe in hallway", dto.GetProperty("title").GetString());
        Assert.Equal(1, dto.GetProperty("status").GetInt32());
        Assert.Equal(3, dto.GetProperty("priority").GetInt32());
        Assert.Equal(world.ResidentA.UserId, dto.GetProperty("reportedBy").GetGuid());
        Assert.Equal(world.BuildingA.BuildingId, dto.GetProperty("buildingId").GetGuid());
        Assert.Equal("Building A", dto.GetProperty("buildingNumber").GetString());
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("updatedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("resolvedAt").ValueKind);
        var reporter = dto.GetProperty("reporter");
        Assert.Equal(new[] { "apartment", "firstName", "lastName", "userId" },
            reporter.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal("A1", reporter.GetProperty("apartment").GetProperty("apartmentNumber").GetString());
        Assert.Equal(0, dto.GetProperty("attachments").GetArrayLength());

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LocatariusDbContext>();
        var stored = await db.Issues.SingleAsync(i => i.IssueId == id);
        Assert.Equal(world.ResidentA.UserId, stored.ReportedBy);
        Assert.Equal(world.BuildingA.BuildingId, stored.BuildingId);
        Assert.Equal(IssueStatus.Open, stored.Status);
        Assert.Equal(IssuePriority.High, stored.Priority);
        Assert.True(stored.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task ResidentCreateDefaultsToMediumPriority()
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, world.ResidentA, csrf: true);

        var response = await client.PostAsJsonAsync("/api/issues",
            new { title = "Broken mailbox", description = "Mailbox door hangs off its hinge." });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("priority").GetInt32());
    }

    [Fact]
    public async Task ResidentListsOnlyOwnBuildingIssuesNewestFirst()
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, world.ResidentA, csrf: true);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/issues",
            new { title = "Newest resident issue", description = "Created during the test run." })).StatusCode);

        var response = await client.GetAsync("/api/issues");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var issues = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(issues);
        // Neighbour's issue in Building A is visible; Building B's issue is not.
        Assert.Equal(new[] { "Newest resident issue", "Neighbour issue A", "Older issue A" },
            issues.Select(i => i.GetProperty("title").GetString()).ToArray());
        Assert.All(issues, i => Assert.Equal(world.BuildingA.BuildingId, i.GetProperty("buildingId").GetGuid()));
        var withAttachment = issues.Single(i => i.GetProperty("title").GetString() == "Older issue A");
        var attachment = withAttachment.GetProperty("attachments")[0];
        Assert.Equal(new[] { "attachmentId", "createdAt", "fileType", "fileUrl", "issueId", "uploadedBy" },
            attachment.EnumerateObject().Select(p => p.Name).Order().ToArray());
    }

    [Fact]
    public async Task ResidentCanViewOwnBuildingIssueButGetsNotFoundForOtherBuilding()
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, world.ResidentA, csrf: false);

        var own = await client.GetAsync($"/api/issues/{world.IssueA.IssueId}");
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(world.IssueA.IssueId, (await own.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("issueId").GetGuid());

        foreach (var id in new[] { world.IssueB.IssueId, Guid.NewGuid() })
        {
            var response = await client.GetAsync($"/api/issues/{id}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            await AssertError(response, "ISSUE_NOT_FOUND");
        }
    }

    [Fact]
    public async Task ResidentCannotPatchIssues()
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, world.ResidentA, csrf: true);

        var response = await Patch(client, world.IssueA.IssueId, new { status = 3 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertError(response, "FORBIDDEN");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LocatariusDbContext>();
        Assert.Equal(IssueStatus.Open, (await db.Issues.SingleAsync(i => i.IssueId == world.IssueA.IssueId)).Status);
    }

    [Fact]
    public async Task UnassignedResidentCannotCreateAndSeesNoIssues()
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, world.UnassignedResident, csrf: true);

        var create = await client.PostAsJsonAsync("/api/issues",
            new { title = "Unassigned report", description = "Should never be stored anywhere." });
        Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);
        await AssertError(create, "APARTMENT_NOT_FOUND");
        Assert.Equal("[]", await (await client.GetAsync("/api/issues")).Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/issues/{world.IssueA.IssueId}")).StatusCode);
        await AssertIssueCount(factory, World.SeededIssueCount);
    }

    [Theory]
    [InlineData("{\"title\":\"Tiny\",\"description\":\"Long enough description.\"}")]
    [InlineData("{\"title\":\"     \",\"description\":\"Long enough description.\"}")]
    [InlineData("{\"title\":\"Bad\\u0007title here\",\"description\":\"Long enough description.\"}")]
    [InlineData("{\"title\":\"Line\\nbreak title\",\"description\":\"Long enough description.\"}")]
    [InlineData("{\"title\":\"Valid title\",\"description\":\"Too short\"}")]
    [InlineData("{\"title\":\"Valid title\",\"description\":\"Has a NUL \\u0000 character\"}")]
    [InlineData("{\"description\":\"Long enough description.\"}")]
    [InlineData("{\"title\":\"Valid title\"}")]
    [InlineData("{\"title\":42,\"description\":\"Long enough description.\"}")]
    [InlineData("{\"title\":\"Valid title\",\"description\":\"Long enough description.\",\"priority\":0}")]
    [InlineData("{\"title\":\"Valid title\",\"description\":\"Long enough description.\",\"priority\":5}")]
    [InlineData("{\"title\":\"Valid title\",\"description\":\"Long enough description.\",\"priority\":\"2\"}")]
    [InlineData("{\"title\":\"Valid title\",\"description\":\"Long enough description.\",\"priority\":2.5}")]
    [InlineData("{\"title\":\"Valid title\",\"description\":\"Long enough description.\",\"reportedBy\":\"00000000-0000-0000-0000-000000000001\"}")]
    [InlineData("{\"title\":\"Valid title\",\"description\":\"Long enough description.\",\"buildingId\":\"00000000-0000-0000-0000-000000000001\"}")]
    [InlineData("{\"title\":\"Valid title\",\"description\":\"Long enough description.\",\"status\":3}")]
    [InlineData("{\"title\":\"Valid title\",\"title\":\"Other title\",\"description\":\"Long enough description.\"}")]
    [InlineData("{\"title\":\"\\uD800 broken\",\"description\":\"Long enough description.\"}")]
    [InlineData("[]")]
    [InlineData("{")]
    public async Task InvalidCreateIsRejectedWithoutPersisting(string body)
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, world.ResidentA, csrf: true);

        var response = await client.PostAsync("/api/issues", Json(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertIssueCount(factory, World.SeededIssueCount);
    }

    [Theory]
    [InlineData(121, 10, HttpStatusCode.BadRequest)]
    [InlineData(120, 2001, HttpStatusCode.BadRequest)]
    [InlineData(120, 2000, HttpStatusCode.Created)]
    [InlineData(5, 10, HttpStatusCode.Created)]
    public async Task CreateEnforcesLengthBoundaries(int titleLength, int descriptionLength, HttpStatusCode expected)
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, world.ResidentA, csrf: true);

        var response = await client.PostAsJsonAsync("/api/issues",
            new { title = new string('t', titleLength), description = new string('d', descriptionLength) });

        Assert.Equal(expected, response.StatusCode);
    }

    // ---------- Admin flows ----------

    [Fact]
    public async Task AdminCannotCreateIssues()
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, world.AdminA, csrf: true);

        var response = await client.PostAsJsonAsync("/api/issues",
            new { title = "Admin issue", description = "Admins triage, they do not report." });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertIssueCount(factory, World.SeededIssueCount);
    }

    [Fact]
    public async Task AdminListsAndViewsOnlyOwnedBuildingIssues()
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, world.AdminA, csrf: false);

        var issues = await client.GetFromJsonAsync<JsonElement[]>("/api/issues");
        Assert.NotNull(issues);
        Assert.Equal(new[] { "Neighbour issue A", "Older issue A" },
            issues.Select(i => i.GetProperty("title").GetString()).ToArray());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/issues/{world.IssueA.IssueId}")).StatusCode);
        var foreign = await client.GetAsync($"/api/issues/{world.IssueB.IssueId}");
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        await AssertError(foreign, "ISSUE_NOT_FOUND");
    }

    [Fact]
    public async Task AdminTriagesOwnIssueThroughResolveAndReopen()
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, world.AdminA, csrf: true);
        var id = world.IssueA.IssueId;

        var progress = await Patch(client, id, new { status = 2, priority = 4 });
        Assert.Equal(HttpStatusCode.OK, progress.StatusCode);
        Assert.True(progress.Headers.CacheControl?.NoStore);
        var dto = await progress.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(IssueFields, dto.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(2, dto.GetProperty("status").GetInt32());
        Assert.Equal(4, dto.GetProperty("priority").GetInt32());
        Assert.NotEqual(JsonValueKind.Null, dto.GetProperty("updatedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("resolvedAt").ValueKind);

        var resolved = await (await Patch(client, id, new { status = 3 })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, resolved.GetProperty("status").GetInt32());
        Assert.NotEqual(JsonValueKind.Null, resolved.GetProperty("resolvedAt").ValueKind);

        var locked = await Patch(client, id, new { priority = 1 });
        Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
        await AssertError(locked, "ISSUE_RESOLVED");

        var reopened = await (await Patch(client, id, new { status = 1, priority = 1 })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, reopened.GetProperty("status").GetInt32());
        Assert.Equal(1, reopened.GetProperty("priority").GetInt32());
        Assert.Equal(JsonValueKind.Null, reopened.GetProperty("resolvedAt").ValueKind);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LocatariusDbContext>();
        var stored = await db.Issues.SingleAsync(i => i.IssueId == id);
        Assert.Equal(IssueStatus.Open, stored.Status);
        Assert.Equal(IssuePriority.Low, stored.Priority);
        Assert.Null(stored.ResolvedAt);
        Assert.NotNull(stored.UpdatedAt);
        Assert.Equal(world.ResidentB.UserId, stored.ReportedBy);
    }

    [Fact]
    public async Task UnchangedPatchDoesNotTouchUpdatedAt()
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, world.AdminA, csrf: true);

        var response = await Patch(client, world.IssueA.IssueId, new { status = 1, priority = 2 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JsonValueKind.Null,
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("updatedAt").ValueKind);
    }

    [Fact]
    public async Task AdminGetsNotFoundPatchingAnotherAdminsIssue()
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, world.AdminA, csrf: true);

        foreach (var id in new[] { world.IssueB.IssueId, Guid.NewGuid() })
        {
            var response = await Patch(client, id, new { status = 3 });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            await AssertError(response, "ISSUE_NOT_FOUND");
        }
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LocatariusDbContext>();
        var foreign = await db.Issues.SingleAsync(i => i.IssueId == world.IssueB.IssueId);
        Assert.Equal(IssueStatus.Open, foreign.Status);
        Assert.Null(foreign.UpdatedAt);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"status\":null,\"priority\":null}")]
    [InlineData("{\"status\":0}")]
    [InlineData("{\"status\":4}")]
    [InlineData("{\"status\":\"Resolved\"}")]
    [InlineData("{\"priority\":9}")]
    [InlineData("{\"status\":2,\"title\":\"Renamed by admin\"}")]
    [InlineData("{\"status\":2,\"status\":3}")]
    [InlineData("[]")]
    public async Task InvalidPatchIsRejectedWithoutChanges(string body)
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, world.AdminA, csrf: true);

        var response = await client.PatchAsync($"/api/issues/{world.IssueA.IssueId}", Json(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LocatariusDbContext>();
        var stored = await db.Issues.SingleAsync(i => i.IssueId == world.IssueA.IssueId);
        Assert.Equal(IssueStatus.Open, stored.Status);
        Assert.Null(stored.UpdatedAt);
    }

    // ---------- Transport, session and CSRF protections ----------

    [Theory]
    [InlineData("GET", "/api/issues")]
    [InlineData("GET", "/api/issues/{id}")]
    [InlineData("POST", "/api/issues")]
    [InlineData("PATCH", "/api/issues/{id}")]
    public async Task EndpointsRequireAnActiveSession(string method, string path)
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await AddCsrf(client);

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method),
            path.Replace("{id}", world.IssueA.IssueId.ToString()))
        {
            Content = method is "POST" or "PATCH" ? Json("{\"status\":3}") : null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertError(response, "UNAUTHENTICATED");
    }

    [Fact]
    public async Task InactiveAndPasswordChangeSessionsAreRejected()
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var inactive = await ClientFor(factory, world.InactiveResident, csrf: false);
        using var restricted = await ClientFor(factory, world.ResidentA, csrf: false, SessionType.PasswordChange);

        Assert.Equal(HttpStatusCode.Unauthorized, (await inactive.GetAsync("/api/issues")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await restricted.GetAsync("/api/issues")).StatusCode);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PATCH")]
    public async Task MutationsRequireCsrfToken(string method)
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        var user = method == "POST" ? world.ResidentA : world.AdminA;
        using var client = await ClientFor(factory, user, csrf: false);
        var path = method == "POST" ? "/api/issues" : $"/api/issues/{world.IssueA.IssueId}";
        var body = method == "POST"
            ? "{\"title\":\"Valid title\",\"description\":\"Long enough description.\"}"
            : "{\"status\":3}";

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path) { Content = Json(body) });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertError(response, "CSRF_INVALID");
        await AssertIssueCount(factory, World.SeededIssueCount);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PATCH")]
    public async Task MutationsRejectOversizedAndNonJsonBodies(string method)
    {
        await using var factory = new IssuesFactory();
        var world = await World.SeedAsync(factory);
        using var client = await ClientFor(factory, method == "POST" ? world.ResidentA : world.AdminA, csrf: true);
        var path = method == "POST" ? "/api/issues" : $"/api/issues/{world.IssueA.IssueId}";

        var tooLarge = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path)
            { Content = Json(new string(' ', 16 * 1024 + 1)) });
        var plainText = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path)
            { Content = new StringContent("{}", Encoding.UTF8, "text/plain") });

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, tooLarge.StatusCode);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, plainText.StatusCode);
    }

    // ---------- Helpers ----------

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    private static Task<HttpResponseMessage> Patch(HttpClient client, Guid id, object body)
        => client.PatchAsJsonAsync($"/api/issues/{id}", body);

    private static async Task AssertError(HttpResponseMessage response, string code)
    {
        var error = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error");
        Assert.Equal(code, error.GetProperty("code").GetString());
    }

    private static async Task AssertIssueCount(IssuesFactory factory, int expected)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LocatariusDbContext>();
        Assert.Equal(expected, await db.Issues.CountAsync());
    }

    private static async Task AddCsrf(HttpClient client)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
    }

    private static async Task<HttpClient> ClientFor(
        IssuesFactory factory, User user, bool csrf, SessionType sessionType = SessionType.Full)
    {
        var token = $"issues-test-{Guid.NewGuid():N}";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LocatariusDbContext>();
            var now = DateTimeOffset.UtcNow;
            db.Sessions.Add(new Session
            {
                SessionId = Guid.NewGuid(), UserId = user.UserId, TokenHash = SessionTokenGenerator.HashToken(token),
                SessionType = sessionType, CreatedAt = now.AddMinutes(-2), LastSeenAt = now.AddMinutes(-1),
                ExpiresAt = now.AddHours(1)
            });
            await db.SaveChangesAsync();
        }
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("Cookie", $"__Host-locatarius={token}");
        if (csrf) await AddCsrf(client);
        return client;
    }

    /// <summary>Admin A owns Building A, Admin B owns Building B. Each building has residents and issues.</summary>
    private sealed record World(
        User AdminA, User AdminB, User ResidentA, User ResidentB, User ResidentOfB,
        User UnassignedResident, User InactiveResident, Building BuildingA, Building BuildingB,
        Issue IssueA, Issue IssueB)
    {
        public const int SeededIssueCount = 3;

        public static async Task<World> SeedAsync(IssuesFactory factory)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LocatariusDbContext>();
            var adminA = NewUser("Admin", "A", "admin@a.example.test", UserRoleType.Admin);
            var adminB = NewUser("Admin", "B", "admin@b.example.test", UserRoleType.Admin);
            var buildingA = NewBuilding(adminA.UserId, "Building A");
            var buildingB = NewBuilding(adminB.UserId, "Building B");
            var apartmentA1 = NewApartment(buildingA, "A1");
            var apartmentA2 = NewApartment(buildingA, "A2");
            var apartmentB1 = NewApartment(buildingB, "B1");
            var residentA = NewUser("Ana", "Popescu", "ana@a.example.test", apartment: apartmentA1);
            var residentB = NewUser("Ion", "Rusu", "ion@a.example.test", apartment: apartmentA2);
            var residentOfB = NewUser("Bogdan", "Lupu", "bogdan@b.example.test", apartment: apartmentB1);
            var unassigned = NewUser("No", "Apartment", "none@a.example.test");
            var inactive = NewUser("Old", "Tenant", "old@a.example.test", apartment: apartmentA1);
            inactive.IsActive = false;
            var now = DateTimeOffset.UtcNow;
            var olderA = NewIssue(residentA, buildingA, "Older issue A", now.AddDays(-2));
            olderA.Attachments.Add(new IssueAttachment
            {
                AttachmentId = Guid.NewGuid(), IssueId = olderA.IssueId, UploadedBy = residentA.UserId,
                FileUrl = "/demo/attachments/photo.jpg", FileType = "image/jpeg", CreatedAt = now.AddDays(-2)
            });
            var issueA = NewIssue(residentB, buildingA, "Neighbour issue A", now.AddDays(-1));
            var issueB = NewIssue(residentOfB, buildingB, "Issue B", now.AddHours(-1));

            db.Users.AddRange(adminA, adminB, residentA, residentB, residentOfB, unassigned, inactive);
            db.Apartments.AddRange(apartmentA1, apartmentA2, apartmentB1);
            db.Issues.AddRange(olderA, issueA, issueB);
            await db.SaveChangesAsync();
            return new(adminA, adminB, residentA, residentB, residentOfB, unassigned, inactive,
                buildingA, buildingB, issueA, issueB);
        }

        private static User NewUser(
            string firstName, string lastName, string email,
            UserRoleType role = UserRoleType.Resident, Apartment? apartment = null)
        {
            var id = Guid.NewGuid();
            return new User
            {
                UserId = id, FirstName = firstName, LastName = lastName, IsActive = true,
                Apartment = apartment, ApartmentId = apartment?.ApartmentId,
                Credential = new UserCredential { UserId = id, Email = email, PasswordHash = "not-returned-secret" },
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

        private static Issue NewIssue(User reporter, Building building, string title, DateTimeOffset createdAt) => new()
        {
            IssueId = Guid.NewGuid(), ReportedBy = reporter.UserId, BuildingId = building.BuildingId,
            Title = title, Description = $"{title} description.", Status = IssueStatus.Open,
            Priority = IssuePriority.Medium, CreatedAt = createdAt
        };
    }

    private sealed class IssuesFactory : WebApplicationFactory<Program>
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
