using Locatarius.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Locatarius.Infrastructure.Persistence;

public sealed class LocatariusDbContext : DbContext
{
    public LocatariusDbContext(
        DbContextOptions<LocatariusDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserContact> UserContacts => Set<UserContact>();
    public DbSet<UserCredential> UserCredentials => Set<UserCredential>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Apartment> Apartments => Set<Apartment>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueAttachment> IssueAttachments => Set<IssueAttachment>();
    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(LocatariusDbContext).Assembly);
    }
}