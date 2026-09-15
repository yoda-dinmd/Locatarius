namespace Locatarius.Domain.Entities;

public sealed class Address
{
    public Guid AddressId { get; set; }

    public string Locality { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public ICollection<Building> Buildings { get; set; } =
        new List<Building>();
}