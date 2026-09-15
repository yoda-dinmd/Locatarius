using Locatarius.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locatarius.Infrastructure.Persistence.Configurations;

public sealed class UserContactConfiguration
    : IEntityTypeConfiguration<UserContact>
{
    public void Configure(EntityTypeBuilder<UserContact> builder)
    {
        builder.ToTable("user_contacts");

        builder.HasKey(contact => contact.UserId);

        builder.Property(contact => contact.UserId)
            .HasColumnName("user_id");

        builder.Property(contact => contact.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(contact => contact.PhoneNumber)
            .IsUnique();

        builder.HasOne(contact => contact.User)
            .WithOne(user => user.Contact)
            .HasForeignKey<UserContact>(contact => contact.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}