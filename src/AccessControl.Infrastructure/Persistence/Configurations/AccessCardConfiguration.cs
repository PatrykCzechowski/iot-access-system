using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessControl.Infrastructure.Persistence.Configurations;

public sealed class AccessCardConfiguration : IEntityTypeConfiguration<AccessCard>
{
    public void Configure(EntityTypeBuilder<AccessCard> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CardUid)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(c => c.CardUid)
            .IsUnique();

        builder.Property(c => c.UserId)
            .HasMaxLength(450);

        builder.HasIndex(c => c.UserId);

        builder.HasIndex(c => c.ZoneId);

        builder.Property(c => c.Label)
            .HasMaxLength(200);

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();
    }
}
