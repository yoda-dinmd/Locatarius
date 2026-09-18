using Locatarius.Domain.Entities;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure;
using Locatarius.Infrastructure.Auth;
using Locatarius.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Locatarius.Infrastructure.Tests;

public sealed class PostgresAuthenticationTests : IAsyncLifetime
{
    private const string Password = "CorrectPermanentPassword123!";
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:18-alpine").Build();
    private Guid userId;
    public async Task InitializeAsync()
    {
        await database.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var user = new User { UserId = Guid.NewGuid(), FirstName = "Test", LastName = "Resident",
            Credential = new UserCredential { Email = "resident@example.test", PasswordHash = new PasswordHasher().HashPassword(Password), MustChangePassword = false },
            Role = new UserRole { Role = UserRoleType.Resident } };
        userId = user.UserId;
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
    public Task DisposeAsync() => database.DisposeAsync().AsTask();

    [Fact]
    public async Task ParallelFailuresPreserveAllIncrementsAndLockout()
    {
        var attempts = Enumerable.Range(0, 5).Select(async _ =>
        {
            await using var db = CreateContext();
            return await Service(db).LoginAsync("resident@example.test", "wrong", default);
        });
        var results = await Task.WhenAll(attempts);
        Assert.All(results, result => Assert.Equal(LoginResultStatus.InvalidCredentials, result.Status));
        await using var check = CreateContext();
        var credential = await check.UserCredentials.SingleAsync();
        Assert.Equal(5, credential.FailedLoginAttempts);
        Assert.True(credential.LockedUntil > DateTimeOffset.UtcNow);
        Assert.Equal(LoginResultStatus.InvalidCredentials,
            (await Service(check).LoginAsync("resident@example.test", Password, default)).Status);
        Assert.Empty(await check.Sessions.ToListAsync());
    }

    [Fact]
    public async Task LoginWaitsForAccountLockAndObservesCommittedFifthFailure()
    {
        await using var owner = CreateContext();
        await using var transaction = await owner.Database.BeginTransactionAsync();
        await owner.Database.ExecuteSqlInterpolatedAsync($"SELECT user_id FROM users WHERE user_id = {userId} FOR UPDATE");
        await using var waiting = CreateContext("waiting-login");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var login = Service(waiting).LoginAsync("resident@example.test", Password, timeout.Token);
        await using var observer = new NpgsqlConnection(database.GetConnectionString());
        await observer.OpenAsync();
        var isWaiting = false;
        for (var i = 0; i < 200; i++)
        {
            await using var command = new NpgsqlCommand(
                "SELECT count(*) FROM pg_stat_activity WHERE application_name = 'waiting-login' AND wait_event_type = 'Lock'",
                observer);
            isWaiting = (long)(await command.ExecuteScalarAsync())! > 0;
            if (isWaiting || login.IsCompleted) break;
            await Task.Delay(25);
        }
        Assert.True(isWaiting, "Login must block on the user row before credential verification.");
        var credential = await owner.UserCredentials.SingleAsync();
        credential.FailedLoginAttempts = 5;
        credential.LockedUntil = DateTimeOffset.UtcNow.AddMinutes(15);
        await owner.SaveChangesAsync();
        await transaction.CommitAsync();
        Assert.Equal(LoginResultStatus.InvalidCredentials, (await login).Status);
        await using var check = CreateContext();
        Assert.Equal(5, (await check.UserCredentials.SingleAsync()).FailedLoginAttempts);
        Assert.Empty(await check.Sessions.ToListAsync());
    }

    [Fact]
    public async Task AcceptedActivityCannotResurrectDeletedSession()
    {
        await using var db = CreateContext();
        var result = await Service(db).LoginAsync("resident@example.test", Password, default);
        var lookup = await Service(db).ResolveSessionAsync(result.SessionToken, default);
        Assert.True(lookup.IsValid);
        Assert.True(await Service(db).RecordAcceptedActivityAsync(lookup.Session!.SessionId, default));
        await using var other = CreateContext();
        await other.Sessions.ExecuteDeleteAsync();
        Assert.False(await Service(db).RecordAcceptedActivityAsync(lookup.Session.SessionId, default));
        Assert.Empty(await other.Sessions.ToListAsync());
    }

    private LocatariusDbContext CreateContext(string applicationName = "auth-tests")
    {
        var connection = new NpgsqlConnectionStringBuilder(database.GetConnectionString()) { ApplicationName = applicationName };
        return new(new DbContextOptionsBuilder<LocatariusDbContext>().UseNpgsql(connection.ConnectionString).Options);
    }
    private static AuthenticationService Service(LocatariusDbContext db) => new(db, new PasswordHasher());
}
