using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessControl.Infrastructure.Persistence.Configurations;

public sealed class AccessZoneConfiguration : IEntityTypeConfiguration<AccessZone>
{
    public void Configure(EntityTypeBuilder<AccessZone> builder)
    {
        builder.HasKey(z => z.Id);

        builder.Property(z => z.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(z => z.Description)
            .HasMaxLength(500);

        // Natural key — unique business identifier
        builder.HasIndex(z => z.Name).IsUnique();
    }
}
