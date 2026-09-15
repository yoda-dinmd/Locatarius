using Locatarius.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locatarius.Infrastructure.Persistence.Configurations;

public sealed class ApartmentConfiguration
    : IEntityTypeConfiguration<Apartment>
{
    public void Configure(EntityTypeBuilder<Apartment> builder)
    {
        builder.ToTable("apartments");

        builder.HasKey(apartment => apartment.ApartmentId);

        builder.Property(apartment => apartment.ApartmentId)
            .HasColumnName("apartment_id");

        builder.Property(apartment => apartment.BuildingId)
            .HasColumnName("building_id")
            .IsRequired();

        builder.Property(apartment => apartment.ApartmentNumber)
            .HasColumnName("apartment_nr")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(apartment => apartment.Floor)
            .HasColumnName("floor")
            .IsRequired();

        builder.HasIndex(apartment => new
        {
            apartment.BuildingId,
            apartment.ApartmentNumber
        })
        .IsUnique();

        builder.HasOne(apartment => apartment.Building)
            .WithMany(building => building.Apartments)
            .HasForeignKey(apartment => apartment.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}