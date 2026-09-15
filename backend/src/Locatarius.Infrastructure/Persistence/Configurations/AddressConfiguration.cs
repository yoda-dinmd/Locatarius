using Locatarius.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locatarius.Infrastructure.Persistence.Configurations;

public sealed class AddressConfiguration
    : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("address");

        builder.HasKey(address => address.AddressId);

        builder.Property(address => address.AddressId)
            .HasColumnName("address_id");

        builder.Property(address => address.Locality)
            .HasColumnName("locality")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(address => address.District)
            .HasColumnName("district")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(address => address.Street)
            .HasColumnName("street")
            .HasMaxLength(200)
            .IsRequired();
    }
}