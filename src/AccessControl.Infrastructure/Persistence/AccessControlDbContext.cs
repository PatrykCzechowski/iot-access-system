using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Persistence;

public class AccessControlDbContext(DbContextOptions<AccessControlDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<AccessCard> AccessCards => Set<AccessCard>();
    public DbSet<AccessZone> AccessZones => Set<AccessZone>();
    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AccessControlDbContext).Assembly);
    }
}
