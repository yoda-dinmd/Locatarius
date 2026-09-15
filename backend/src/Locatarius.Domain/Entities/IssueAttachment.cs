namespace Locatarius.Domain.Entities;

public sealed class IssueAttachment
{
    public Guid AttachmentId { get; set; }

    public Guid IssueId { get; set; }

    public Guid UploadedBy { get; set; }

    public string FileUrl { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public Issue Issue { get; set; } = null!;

    public User Uploader { get; set; } = null!;
}