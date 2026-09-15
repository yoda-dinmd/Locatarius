namespace Locatarius.Domain.Entities;

public sealed class UserContact
{
    public Guid UserId { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}