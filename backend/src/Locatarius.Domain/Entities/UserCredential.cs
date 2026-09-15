namespace Locatarius.Domain.Entities;

public sealed class UserCredential
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool MustChangePassword { get; set; } = true;

    public DateTimeOffset? PasswordChangedAt { get; set; }

    public User User { get; set; } = null!;
}