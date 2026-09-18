using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Locatarius.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Locatarius.Infrastructure.Tests;

public sealed class AuthHttpTests : IAsyncLifetime
{
    private const string Password = "CorrectTemporaryPassword123!";
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:18-alpine").Build();
    public Task InitializeAsync() => database.StartAsync();
    public Task DisposeAsync() => database.DisposeAsync().AsTask();

    [Fact]
    public async Task CookiesCsrfValidationLoginLogoutAndRateLimitWorkOverHttps()
    {
        await using var factory = new ApiFactory(database.GetConnectionString());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var csrf = await client.GetAsync("/api/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        var csrfCookie = csrf.Headers.GetValues("Set-Cookie").Single();
        AssertCookieFlags(csrfCookie, "__Host-locatarius-csrf");
        var token = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Add("Cookie", csrfCookie.Split(';')[0]);
        var denied = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@example.test", password = Password });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
        var invalid = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@example.test", password = Password, role = "admin" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var missing = await client.PostAsJsonAsync("/api/auth/login", new { email = (string?)null, password = (string?)null });
        Assert.Contains("VALIDATION_FAILED", await missing.Content.ReadAsStringAsync());
        var wrong = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@example.test", password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Contains("INVALID_CREDENTIALS", await wrong.Content.ReadAsStringAsync());
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@example.test", password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal("change_password", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("next").GetString());
        var sessionCookie = login.Headers.GetValues("Set-Cookie").Single();
        AssertCookieFlags(sessionCookie, "__Host-locatarius=");
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", csrfCookie.Split(';')[0] + "; " + sessionCookie.Split(';')[0]);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var logoutDenied = await client.PostAsJsonAsync("/api/auth/logout", new { });
        Assert.Equal(HttpStatusCode.Forbidden, logoutDenied.StatusCode);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
        var logout = await client.PostAsJsonAsync("/api/auth/logout", new { });
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        // Replay the same pre-logout credential, not an automatically cleared cookie jar.
        var replay = await client.PostAsJsonAsync("/api/auth/logout", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        // Five login requests above consumed budget, including rejected CSRF/validation.
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/auth/login", new { })).StatusCode);
        var limited = await client.PostAsJsonAsync("/api/auth/login", new { });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.NotNull(limited.Headers.RetryAfter);
    }

    [Fact]
    public async Task ChunkedOversizedBodyStopsReadingAtLimitPlusOne()
    {
        await using var factory = new ApiFactory(database.GetConnectionString());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var body = new CountingStream(1024 * 1024);
        var response = await client.PostAsync("/api/auth/login", new StreamingContent(body));
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Contains("PAYLOAD_TOO_LARGE", await response.Content.ReadAsStringAsync());
        // Direct controller probe below checks consumption independently of transport buffering.
        await using var scope = factory.Services.CreateAsyncScope();
        var controller = ActivatorUtilities.CreateInstance<Locatarius.Api.Controllers.AuthController>(scope.ServiceProvider);
        using var directBody = new CountingStream(1024 * 1024);
        var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = directBody;
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = context };
        var result = Assert.IsType<Microsoft.AspNetCore.Mvc.ObjectResult>(await controller.Login(default));
        Assert.Equal(413, result.StatusCode);
        Assert.Equal(16 * 1024 + 1, directBody.BytesRead);
    }

    [Theory]
    [InlineData("172.28.0.10", "172.28.0.10", true)]
    [InlineData("172.28.0.11", "172.28.0.10", false)]
    [InlineData("172.28.0.10", "", false)]
    public async Task OnlyConfiguredProxyCanForwardHttps(string remote, string trusted, bool accepted)
    {
        await using var factory = new ApiFactory(database.GetConnectionString(), remote, trusted);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.8");
        var response = await client.GetAsync("/api/auth/csrf");
        Assert.Equal(accepted ? HttpStatusCode.OK : HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Theory]
    [InlineData("172.28.0.10", true)]
    [InlineData("172.28.0.11", false)]
    public async Task OnlyTrustedProxyCanSelectClientRateLimitPartition(string remote, bool trusted)
    {
        await using var factory = new ApiFactory(database.GetConnectionString(), remote, "172.28.0.10");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.8");
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/auth/login", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/auth/login", new { })).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Forwarded-For");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.9");
        Assert.Equal(trusted ? HttpStatusCode.Forbidden : HttpStatusCode.TooManyRequests,
            (await client.PostAsJsonAsync("/api/auth/login", new { })).StatusCode);
    }

    private static void AssertCookieFlags(string cookie, string name)
    {
        Assert.StartsWith(name, cookie);
        var lower = cookie.ToLowerInvariant();
        Assert.Contains("secure", lower);
        Assert.Contains("httponly", lower);
        Assert.Contains("samesite=lax", lower);
        Assert.Contains("path=/", lower);
        Assert.DoesNotContain("domain=", lower);
    }

    private sealed class ApiFactory(string connection, string remote = "127.0.0.1", string proxy = "")
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connection,
                ["SeedAdmin:Email"] = "admin@example.test", ["SeedAdmin:Password"] = Password,
                ["SeedAdmin:FirstName"] = "Test", ["SeedAdmin:LastName"] = "Admin",
                ["Proxy:TrustedProxy"] = proxy
            }));
            builder.ConfigureTestServices(services => services.AddSingleton<IStartupFilter>(new AddressFilter(remote)));
        }
    }
    private sealed class AddressFilter(string address) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(address);
                await nextMiddleware();
            });
            next(app);
        };
    }
    private sealed class CountingStream(int length) : Stream
    {
        public int BytesRead { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => BytesRead; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var size = Math.Min(count, length - BytesRead);
            Array.Fill(buffer, (byte)' ', offset, size);
            BytesRead += size;
            return size;
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var size = Math.Min(buffer.Length, length - BytesRead);
            buffer.Span[..size].Fill((byte)' ');
            BytesRead += size;
            return ValueTask.FromResult(size);
        }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
    private sealed class StreamingContent(Stream source) : HttpContent
    {
        public StreamingContent(CountingStream source) : this((Stream)source)
            => Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => source.CopyToAsync(stream);
    }
}
