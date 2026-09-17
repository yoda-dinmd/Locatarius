using Locatarius.Infrastructure;
using Locatarius.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<LocatariusDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<DemoDataSeeder>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<LocatariusDbContext>();

    await dbContext.Database.MigrateAsync();

    var seeder = scope.ServiceProvider
        .GetRequiredService<DatabaseSeeder>();

    await seeder.SeedAsync();

    var demoDataSeeder = scope.ServiceProvider
        .GetRequiredService<DemoDataSeeder>();

    await demoDataSeeder.SeedAsync();
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

app.MapHealthChecks("/health/live");

app.Run();