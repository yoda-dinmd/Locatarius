using Locatarius.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locatarius.Infrastructure.Persistence.Configurations;

public sealed class IssueConfiguration
    : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.ToTable("issues");

        builder.HasKey(issue => issue.IssueId);

        builder.Property(issue => issue.IssueId)
            .HasColumnName("issue_id");

        builder.Property(issue => issue.ReportedBy)
            .HasColumnName("reported_by")
            .IsRequired();

        builder.Property(issue => issue.BuildingId)
            .HasColumnName("building_id")
            .IsRequired();

        builder.Property(issue => issue.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(issue => issue.Description)
            .HasColumnName("description")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(issue => issue.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(issue => issue.Priority)
            .HasColumnName("priority")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(issue => issue.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(issue => issue.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(issue => issue.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.HasOne(issue => issue.Reporter)
            .WithMany(user => user.ReportedIssues)
            .HasForeignKey(issue => issue.ReportedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(issue => issue.Building)
            .WithMany(building => building.Issues)
            .HasForeignKey(issue => issue.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}