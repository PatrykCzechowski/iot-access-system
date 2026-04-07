using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessControl.Infrastructure.Persistence.Configurations;

public class CardholderConfiguration : IEntityTypeConfiguration<Cardholder>
{
    public void Configure(EntityTypeBuilder<Cardholder> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .HasMaxLength(100);

        builder.Property(c => c.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.HasMany(c => c.Cards)
            .WithOne(c => c.Cardholder)
            .HasForeignKey(c => c.CardholderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.AccessProfiles)
            .WithMany(p => p.Cardholders)
            .UsingEntity("CardholderAccessProfile");
    }
}
