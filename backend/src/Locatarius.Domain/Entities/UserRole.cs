using Locatarius.Domain.Enums;

namespace Locatarius.Domain.Entities;

public sealed class UserRole
{
    public Guid UserId { get; set; }

    public UserRoleType Role { get; set; }

    public User User { get; set; } = null!;
}