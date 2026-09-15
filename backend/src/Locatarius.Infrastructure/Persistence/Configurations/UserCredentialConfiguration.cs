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

        builder.HasIndex(credential => credential.Email)
            .IsUnique();

        builder.Property(credential => credential.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();

        builder.Property(credential => credential.MustChangePassword)
            .HasColumnName("must_change_password")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(credential => credential.PasswordChangedAt)
            .HasColumnName("password_changed_at");

        builder.HasOne(credential => credential.User)
            .WithOne(user => user.Credential)
            .HasForeignKey<UserCredential>(credential => credential.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}