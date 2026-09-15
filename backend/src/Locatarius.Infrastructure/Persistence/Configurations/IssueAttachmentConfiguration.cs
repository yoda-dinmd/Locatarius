using Locatarius.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locatarius.Infrastructure.Persistence.Configurations;

public sealed class IssueAttachmentConfiguration
    : IEntityTypeConfiguration<IssueAttachment>
{
    public void Configure(EntityTypeBuilder<IssueAttachment> builder)
    {
        builder.ToTable("issue_attachments");

        builder.HasKey(attachment => attachment.AttachmentId);

        builder.Property(attachment => attachment.AttachmentId)
            .HasColumnName("attachment_id");

        builder.Property(attachment => attachment.IssueId)
            .HasColumnName("issue_id")
            .IsRequired();

        builder.Property(attachment => attachment.UploadedBy)
            .HasColumnName("uploaded_by")
            .IsRequired();

        builder.Property(attachment => attachment.FileUrl)
            .HasColumnName("file_url")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(attachment => attachment.FileType)
            .HasColumnName("file_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(attachment => attachment.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasOne(attachment => attachment.Issue)
            .WithMany(issue => issue.Attachments)
            .HasForeignKey(attachment => attachment.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(attachment => attachment.Uploader)
            .WithMany(user => user.UploadedAttachments)
            .HasForeignKey(attachment => attachment.UploadedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}