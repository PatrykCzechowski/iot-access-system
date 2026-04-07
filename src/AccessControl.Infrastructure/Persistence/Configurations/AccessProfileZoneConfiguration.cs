using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessControl.Infrastructure.Persistence.Configurations;

public class AccessProfileZoneConfiguration : IEntityTypeConfiguration<AccessProfileZone>
{
    public void Configure(EntityTypeBuilder<AccessProfileZone> builder)
    {
        builder.HasKey(apz => new { apz.AccessProfileId, apz.AccessZoneId });

        builder.HasOne(apz => apz.AccessProfile)
            .WithMany(ap => ap.AccessProfileZones)
            .HasForeignKey(apz => apz.AccessProfileId);

        builder.HasOne(apz => apz.AccessZone)
            .WithMany(az => az.AccessProfileZones)
            .HasForeignKey(apz => apz.AccessZoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
