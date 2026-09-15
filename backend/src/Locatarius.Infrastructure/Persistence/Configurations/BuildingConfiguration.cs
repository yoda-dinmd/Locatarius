using Locatarius.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locatarius.Infrastructure.Persistence.Configurations;

public sealed class BuildingConfiguration
    : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("buildings");

        builder.HasKey(building => building.BuildingId);

        builder.Property(building => building.BuildingId)
            .HasColumnName("building_id");

        builder.Property(building => building.AdminId)
            .HasColumnName("admin_id")
            .IsRequired();

        builder.Property(building => building.BuildingNumber)
            .HasColumnName("building_nr")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(building => building.NumberOfFloors)
            .HasColumnName("number_of_floors")
            .IsRequired();

        builder.Property(building => building.AddressId)
            .HasColumnName("address_id")
            .IsRequired();

        builder.HasOne(building => building.Admin)
            .WithMany()
            .HasForeignKey(building => building.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(building => building.Address)
            .WithMany(address => address.Buildings)
            .HasForeignKey(building => building.AddressId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}