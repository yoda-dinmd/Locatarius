using Locatarius.Domain.Enums;

namespace Locatarius.Domain.Entities;

public sealed class Issue
{
    public Guid IssueId { get; set; }

    public Guid ReportedBy { get; set; }

    public Guid BuildingId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IssueStatus Status { get; set; } = IssueStatus.Open;

    public IssuePriority Priority { get; set; } = IssuePriority.Medium;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public User Reporter { get; set; } = null!;

    public Building Building { get; set; } = null!;

    public ICollection<IssueAttachment> Attachments { get; set; } =
        new List<IssueAttachment>();
}