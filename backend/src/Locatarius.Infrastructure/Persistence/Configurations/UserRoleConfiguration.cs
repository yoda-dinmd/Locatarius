using Locatarius.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locatarius.Infrastructure.Persistence.Configurations;

public sealed class UserRoleConfiguration
    : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_role");

        builder.HasKey(userRole => userRole.UserId);

        builder.Property(userRole => userRole.UserId)
            .HasColumnName("user_id");

        builder.Property(userRole => userRole.Role)
            .HasColumnName("role")
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(userRole => userRole.User)
            .WithOne(user => user.Role)
            .HasForeignKey<UserRole>(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}