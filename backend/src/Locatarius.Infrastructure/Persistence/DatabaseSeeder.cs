using Locatarius.Domain.Entities;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Locatarius.Infrastructure.Persistence;

public sealed class DatabaseSeeder(
    LocatariusDbContext dbContext,
    IConfiguration configuration,
    PasswordHasher passwordHasher)
{
    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = configuration.GetSection("SeedAdmin");

        var email = settings["Email"]?
            .Trim()
            .ToLowerInvariant();

        var password = settings["Password"];
        var firstName = settings["FirstName"]?.Trim();
        var lastName = settings["LastName"]?.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "SeedAdmin:Email is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "SeedAdmin:Password is required.");
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new InvalidOperationException(
                "SeedAdmin:FirstName is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new InvalidOperationException(
                "SeedAdmin:LastName is required.");
        }

        var existingCredential = await dbContext.UserCredentials
            .Include(credential => credential.User)
            .ThenInclude(user => user.Role)
            .SingleOrDefaultAsync(
                credential => credential.Email == email,
                cancellationToken);

        if (existingCredential is not null)
        {
            if (existingCredential.User is null)
            {
                throw new InvalidOperationException(
                    "The configured administrator credential has no user record.");
            }

            if (existingCredential.User.Role is null)
            {
                throw new InvalidOperationException(
                    "The configured administrator account has no role record.");
            }

            if (existingCredential.User.Role.Role != UserRoleType.Admin)
            {
                throw new InvalidOperationException(
                    "The configured administrator email belongs to a non-admin account.");
            }

            if (string.IsNullOrWhiteSpace(
                    existingCredential.PasswordHash))
            {
                throw new InvalidOperationException(
                    "The configured administrator account has incomplete credentials.");
            }

            return;
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Credential = new UserCredential
            {
                Email = email,
                PasswordHash = passwordHasher.HashPassword(password),
                MustChangePassword = true
            },
            Role = new UserRole
            {
                Role = UserRoleType.Admin
            },
            Contact = new UserContact
            {
                PhoneNumber = "+37369100000"
            }
        };

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}