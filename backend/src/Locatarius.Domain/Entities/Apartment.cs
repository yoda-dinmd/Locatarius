namespace Locatarius.Domain.Entities;

public sealed class Apartment
{
    public Guid ApartmentId { get; set; }

    public Guid BuildingId { get; set; }

    public string ApartmentNumber { get; set; } = string.Empty;

    public int Floor { get; set; }

    public Building Building { get; set; } = null!;

    public ICollection<User> Residents { get; set; } =
        new List<User>();
}