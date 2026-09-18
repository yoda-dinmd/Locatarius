using Locatarius.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locatarius.Infrastructure.Persistence.Configurations;

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(session => session.SessionId);

        builder.Property(session => session.SessionId)
            .HasColumnName("session_id");

        builder.Property(session => session.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(session => session.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64) // hex-encoded SHA-256 digest
            .IsRequired();

        builder.Property(session => session.SessionType)
            .HasColumnName("session_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(session => session.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(session => session.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(session => session.LastSeenAt)
            .HasColumnName("last_seen_at");

        builder.Property(session => session.RevokedAt)
            .HasColumnName("revoked_at");

        builder.HasIndex(session => session.TokenHash)
            .IsUnique();

        builder.HasIndex(session => session.UserId);

        builder.HasOne(session => session.User)
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}