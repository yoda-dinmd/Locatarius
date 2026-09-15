using Locatarius.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locatarius.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.UserId);

        builder.Property(user => user.UserId)
            .HasColumnName("user_id");

        builder.Property(user => user.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.DateOfBirth)
            .HasColumnName("date_of_birth");

        builder.Property(user => user.ApartmentId)
            .HasColumnName("apartment_id");

        builder.HasOne(user => user.Apartment)
            .WithMany(apartment => apartment.Residents)
            .HasForeignKey(user => user.ApartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}