using Locatarius.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locatarius.Infrastructure.Persistence.Configurations;

public sealed class OtpCodeConfiguration
    : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("otp_codes");

        builder.HasKey(otp => otp.OtpId);

        builder.Property(otp => otp.OtpId)
            .HasColumnName("otp_id");

        builder.Property(otp => otp.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(otp => otp.CodeHash)
            .HasColumnName("code_hash")
            .IsRequired();

        builder.Property(otp => otp.Purpose)
            .HasColumnName("purpose")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(otp => otp.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(otp => otp.UsedAt)
            .HasColumnName("used_at");

        builder.Property(otp => otp.Attempts)
            .HasColumnName("attempts")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(otp => otp.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(otp => new { otp.UserId, otp.Purpose });

        builder.HasOne(otp => otp.User)
            .WithMany(user => user.OtpCodes)
            .HasForeignKey(otp => otp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}