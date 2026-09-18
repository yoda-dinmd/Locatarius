using Locatarius.Domain.Entities;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Locatarius.Infrastructure.Persistence;

public sealed class DemoDataSeeder(
    LocatariusDbContext dbContext,
    IConfiguration configuration,
    PasswordHasher passwordHasher)
{
    private static readonly Guid BuildingOneId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static readonly Guid BuildingTwoId =
        Guid.Parse("10000000-0000-0000-0000-000000000002");

    private static readonly Guid ApartmentOneId =
        Guid.Parse("20000000-0000-0000-0000-000000000001");

    private static readonly Guid ApartmentTwoId =
        Guid.Parse("20000000-0000-0000-0000-000000000002");

    private static readonly Guid ApartmentThreeId =
        Guid.Parse("20000000-0000-0000-0000-000000000003");

    private static readonly Guid ApartmentFourId =
        Guid.Parse("20000000-0000-0000-0000-000000000004");

    private static readonly Guid ApartmentFiveId =
        Guid.Parse("20000000-0000-0000-0000-000000000005");

    private static readonly Guid ApartmentSixId =
        Guid.Parse("20000000-0000-0000-0000-000000000006");

    private static readonly Guid ApartmentSevenId =
        Guid.Parse("20000000-0000-0000-0000-000000000007");

    private static readonly Guid ResidentOneId =
        Guid.Parse("30000000-0000-0000-0000-000000000001");

    private static readonly Guid ResidentTwoId =
        Guid.Parse("30000000-0000-0000-0000-000000000002");

    private static readonly Guid ResidentThreeId =
        Guid.Parse("30000000-0000-0000-0000-000000000003");

    private static readonly Guid ResidentFourId =
        Guid.Parse("30000000-0000-0000-0000-000000000004");

    private static readonly Guid ResidentFiveId =
        Guid.Parse("30000000-0000-0000-0000-000000000005");

    private static readonly Guid ResidentSixId =
        Guid.Parse("30000000-0000-0000-0000-000000000006");

    private static readonly Guid ResidentSevenId =
        Guid.Parse("30000000-0000-0000-0000-000000000007");

    public async Task SeedAsync(
        CancellationToken cancellationToken = default)
    {
        var enabled = bool.TryParse(
            configuration["SeedDemoData:Enabled"],
            out var configuredEnabled)
            && configuredEnabled;

        if (!enabled)
        {
            return;
        }

        var password = SeedSettings.RequireCreationPassword(
            "SeedDemoData:Password",
            configuration["SeedDemoData:Password"]);

        var adminEmail = SeedSettings.RequireNormalizedEmail(
            "SeedAdmin:Email",
            configuration["SeedAdmin:Email"]);

        var adminCredential = await dbContext.UserCredentials
            .SingleOrDefaultAsync(
                credential => credential.Email == adminEmail,
                cancellationToken);

        if (adminCredential is null)
        {
            throw new InvalidOperationException(
                "The configured administrator must be seeded before demo data.");
        }

        var demoUserIds = new[]
        {
            ResidentOneId,
            ResidentTwoId,
            ResidentThreeId,
            ResidentFourId,
            ResidentFiveId,
            ResidentSixId,
            ResidentSevenId
        };

        var existingDemoUsers = await dbContext.Users
            .Where(user => demoUserIds.Contains(user.UserId))
            .Select(user => user.UserId)
            .ToListAsync(cancellationToken);

        if (existingDemoUsers.Count == demoUserIds.Length)
        {
            return;
        }

        if (existingDemoUsers.Count > 0)
        {
            throw new InvalidOperationException(
                "The demo data is incomplete. Reset the development database before reseeding.");
        }

        var now = DateTimeOffset.UtcNow;

        var addressOne = new Address
        {
            AddressId = Guid.Parse(
                "40000000-0000-0000-0000-000000000001"),
            Locality = "Chisinau",
            District = "Centru",
            Street = "Strada Stefan cel Mare 21"
        };

        var addressTwo = new Address
        {
            AddressId = Guid.Parse(
                "40000000-0000-0000-0000-000000000002"),
            Locality = "Chisinau",
            District = "Buiucani",
            Street = "Strada Ion Creanga 45"
        };

        var buildingOne = new Building
        {
            BuildingId = BuildingOneId,
            AdminId = adminCredential.UserId,
            BuildingNumber = "A",
            NumberOfFloors = 3,
            AddressId = addressOne.AddressId
        };

        var buildingTwo = new Building
        {
            BuildingId = BuildingTwoId,
            AdminId = adminCredential.UserId,
            BuildingNumber = "B",
            NumberOfFloors = 3,
            AddressId = addressTwo.AddressId
        };

        var apartments = new[]
        {
            new Apartment
            {
                ApartmentId = ApartmentOneId,
                BuildingId = BuildingOneId,
                ApartmentNumber = "101",
                Floor = 1
            },
            new Apartment
            {
                ApartmentId = ApartmentTwoId,
                BuildingId = BuildingOneId,
                ApartmentNumber = "102",
                Floor = 1
            },
            new Apartment
            {
                ApartmentId = ApartmentThreeId,
                BuildingId = BuildingOneId,
                ApartmentNumber = "201",
                Floor = 2
            },
            new Apartment
            {
                ApartmentId = ApartmentFourId,
                BuildingId = BuildingOneId,
                ApartmentNumber = "202",
                Floor = 2
            },
            new Apartment
            {
                ApartmentId = ApartmentFiveId,
                BuildingId = BuildingTwoId,
                ApartmentNumber = "301",
                Floor = 3
            },
            new Apartment
            {
                ApartmentId = ApartmentSixId,
                BuildingId = BuildingTwoId,
                ApartmentNumber = "302",
                Floor = 3
            },
            new Apartment
            {
                ApartmentId = ApartmentSevenId,
                BuildingId = BuildingTwoId,
                ApartmentNumber = "303",
                Floor = 3
            }
        };

        var residents = new[]
        {
            CreateResident(
                ResidentOneId,
                "Ana",
                "Rusu",
                "ana.rusu@locatarius.md",
                "+37368100001",
                ApartmentOneId,
                password),

            CreateResident(
                ResidentTwoId,
                "Mihai",
                "Ceban",
                "mihai.ceban@locatarius.md",
                "+37360100002",
                ApartmentTwoId,
                password),

            CreateResident(
                ResidentThreeId,
                "Sofia",
                "Munteanu",
                "sofia.munteanu@locatarius.md",
                "+37367100003",
                ApartmentThreeId,
                password),

            CreateResident(
                ResidentFourId,
                "Andrei",
                "Ciobanu",
                "andrei.ciobanu@locatarius.md",
                "+37369100004",
                ApartmentFourId,
                password),

            CreateResident(
                ResidentFiveId,
                "Elena",
                "Rotari",
                "elena.rotari@locatarius.md",
                "+37378100005",
                ApartmentFiveId,
                password),

            CreateResident(
                ResidentSixId,
                "Victor",
                "Balan",
                "victor.balan@locatarius.md",
                "+37368100006",
                ApartmentSixId,
                password),

            CreateResident(
                ResidentSevenId,
                "Irina",
                "Lupu",
                "irina.lupu@locatarius.md",
                "+37360100007",
                ApartmentSevenId,
                password)
        };

        var issues = new[]
        {
            new Issue
            {
                IssueId = Guid.Parse(
                    "50000000-0000-0000-0000-000000000001"),
                ReportedBy = ResidentOneId,
                BuildingId = BuildingOneId,
                Title = "Broken entrance light",
                Description = "The entrance light does not turn on.",
                Status = IssueStatus.Open,
                Priority = IssuePriority.High,
                CreatedAt = now
            },
            new Issue
            {
                IssueId = Guid.Parse(
                    "50000000-0000-0000-0000-000000000002"),
                ReportedBy = ResidentTwoId,
                BuildingId = BuildingOneId,
                Title = "Water pressure problem",
                Description = "Water pressure is low in the bathroom.",
                Status = IssueStatus.InProgress,
                Priority = IssuePriority.Medium,
                CreatedAt = now
            },
            new Issue
            {
                IssueId = Guid.Parse(
                    "50000000-0000-0000-0000-000000000003"),
                ReportedBy = ResidentFourId,
                BuildingId = BuildingTwoId,
                Title = "Heating not working",
                Description = "The heating is not working correctly.",
                Status = IssueStatus.Resolved,
                Priority = IssuePriority.Critical,
                CreatedAt = now,
                ResolvedAt = now
            }
        };

        var attachments = new[]
        {
            new IssueAttachment
            {
                AttachmentId = Guid.Parse(
                    "60000000-0000-0000-0000-000000000001"),
                IssueId = issues[0].IssueId,
                UploadedBy = ResidentOneId,
                FileUrl = "/demo/attachments/entrance-light.jpg",
                FileType = "image/jpeg",
                CreatedAt = now
            },
            new IssueAttachment
            {
                AttachmentId = Guid.Parse(
                    "60000000-0000-0000-0000-000000000002"),
                IssueId = issues[1].IssueId,
                UploadedBy = ResidentTwoId,
                FileUrl = "/demo/attachments/water-pressure.pdf",
                FileType = "application/pdf",
                CreatedAt = now
            }
        };

        var otpCodes = new[]
        {
            new OtpCode
            {
                OtpId = Guid.Parse(
                    "70000000-0000-0000-0000-000000000001"),
                UserId = ResidentOneId,
                CodeHash = "demo-expired-code",
                Purpose = OtpPurpose.EmailVerification,
                ExpiresAt = now.AddMinutes(-10),
                CreatedAt = now.AddMinutes(-20),
                Attempts = 0
            }
        };

        dbContext.AddRange(addressOne, addressTwo);
        dbContext.AddRange(buildingOne, buildingTwo);
        dbContext.AddRange(apartments);
        dbContext.AddRange(residents);
        dbContext.AddRange(issues);
        dbContext.AddRange(attachments);
        dbContext.AddRange(otpCodes);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private User CreateResident(
        Guid userId,
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        Guid apartmentId,
        string password)
    {
        return new User
        {
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            ApartmentId = apartmentId,
            Credential = new UserCredential
            {
                Email = email,
                PasswordHash = passwordHasher.HashPassword(password),
                MustChangePassword = true
            },
            Role = new UserRole
            {
                Role = UserRoleType.Resident
            },
            Contact = new UserContact
            {
                PhoneNumber = phoneNumber
            }
        };
    }
}