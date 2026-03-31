using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessControl.Infrastructure.Persistence.Configurations;

public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.HardwareId)
            .IsRequired();

        builder.HasIndex(d => d.HardwareId)
            .IsUnique();

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.AdapterType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Features)
            .HasConversion<int>();

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .IsRequired();

        builder.Property<Dictionary<string, string>>("_configuration")
            .HasField("_configuration")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("configuration")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.HasIndex(d => d.ZoneId);

        builder.HasOne<AccessZone>()
            .WithMany()
            .HasForeignKey(d => d.ZoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
