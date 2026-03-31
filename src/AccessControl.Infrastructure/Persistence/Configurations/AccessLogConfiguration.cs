using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessControl.Infrastructure.Persistence.Configurations;

public sealed class AccessLogConfiguration : IEntityTypeConfiguration<AccessLog>
{
    public void Configure(EntityTypeBuilder<AccessLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.CardUid)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(l => l.ZoneName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.DeviceName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.UserName)
            .HasMaxLength(200);

        builder.Property(l => l.Message)
            .HasMaxLength(500);

        builder.Property(l => l.AccessGranted)
            .IsRequired();

        builder.Property(l => l.Timestamp)
            .IsRequired();

        builder.HasIndex(l => l.Timestamp).IsDescending();
        builder.HasIndex(l => l.DeviceId);
        builder.HasIndex(l => l.ZoneId);
        builder.HasIndex(l => l.CardUid);

        // No FK to Device/Zone/AccessCard — audit log intentionally survives entity deletion.
        // Snapshot fields (ZoneName, DeviceName, UserName) preserve context for deleted entities.
    }
}
