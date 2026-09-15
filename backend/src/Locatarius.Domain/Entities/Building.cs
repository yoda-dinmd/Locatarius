namespace Locatarius.Domain.Entities;

public sealed class Building
{
    public Guid BuildingId { get; set; }

    public Guid AdminId { get; set; }

    public string BuildingNumber { get; set; } = string.Empty;

    public int NumberOfFloors { get; set; }

    public Guid AddressId { get; set; }

    public User Admin { get; set; } = null!;

    public Address Address { get; set; } = null!;

    public ICollection<Apartment> Apartments { get; set; } =
        new List<Apartment>();

    public ICollection<Issue> Issues { get; set; } =
        new List<Issue>();
}