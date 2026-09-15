using Locatarius.Domain.Enums;

namespace Locatarius.Domain.Entities;

public sealed class User
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public Guid? ApartmentId { get; set; }

    public UserContact? Contact { get; set; }

    public UserCredential? Credential { get; set; }

    public UserRole? Role { get; set; }

    public Apartment? Apartment { get; set; }

    public ICollection<OtpCode> OtpCodes { get; set; } = new List<OtpCode>();

    public ICollection<Issue> ReportedIssues { get; set; } = new List<Issue>();

    public ICollection<IssueAttachment> UploadedAttachments { get; set; } =
        new List<IssueAttachment>();
}