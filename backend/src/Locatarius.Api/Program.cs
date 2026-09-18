using System.Text.Json;
using System.Threading.RateLimiting;
using Locatarius.Infrastructure;
using Locatarius.Infrastructure.Auth;
using Locatarius.Infrastructure.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();

builder.Services.AddDbContext<LocatariusDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<AuthenticationService>();

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "__Host-locatarius-csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Path = "/";
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(60),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var retryAfterSeconds = 60;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
        }

        context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

        var payload = JsonSerializer.Serialize(new
        {
            error = new
            {
                code = "RATE_LIMITED",
                message = "Too many requests. Try again later.",
                fields = new Dictionary<string, string[]>()
            }
        });

        await context.HttpContext.Response.WriteAsync(payload, cancellationToken);
    };
});

var app = builder.Build();

// Safety net: never leak exception details through the /api contract.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new
            {
                error = new
                {
                    code = "INTERNAL_ERROR",
                    message = "An unexpected error occurred.",
                    fields = new Dictionary<string, string[]>()
                }
            });

            await context.Response.WriteAsync(payload);
        }
    }
});

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<LocatariusDbContext>();

    await dbContext.Database.MigrateAsync();

    var seeder = scope.ServiceProvider
        .GetRequiredService<DatabaseSeeder>();

    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseWhen(
    context => !context.Request.Path.Equals("/health/live"),
    branch =>
    {
        branch.UseHttpsRedirection();
    });

app.UseRateLimiter();

app.MapHealthChecks("/health/live");
app.MapControllers();

app.Run();