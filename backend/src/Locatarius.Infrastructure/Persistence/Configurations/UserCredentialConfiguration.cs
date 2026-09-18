using Locatarius.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locatarius.Infrastructure.Persistence.Configurations;

public sealed class UserCredentialConfiguration
    : IEntityTypeConfiguration<UserCredential>
{
    public void Configure(EntityTypeBuilder<UserCredential> builder)
    {
        builder.ToTable("user_credentials");

        builder.HasKey(credential => credential.UserId);

        builder.Property(credential => credential.UserId)
            .HasColumnName("user_id");

        builder.Property(credential => credential.Email)
            .HasColumnName("email")
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(credential => credential.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(credential => credential.MustChangePassword)
            .HasColumnName("must_change_password")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(credential => credential.PasswordChangedAt)
            .HasColumnName("password_changed_at");

        builder.Property(credential => credential.FailedLoginAttempts)
            .HasColumnName("failed_login_attempts")
            .HasDefaultValue(0)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(credential => credential.LockedUntil)
            .HasColumnName("locked_until")
            .IsConcurrencyToken();

        builder.HasIndex(credential => credential.Email)
            .IsUnique();

        builder.HasOne(credential => credential.User)
            .WithOne(user => user.Credential)
            .HasForeignKey<UserCredential>(credential => credential.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}