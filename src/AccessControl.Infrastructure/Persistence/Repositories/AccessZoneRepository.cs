using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Persistence.Repositories;

public sealed class AccessZoneRepository(AccessControlDbContext context) : IAccessZoneRepository
{
    public async Task<IReadOnlyCollection<AccessZone>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.AccessZones
            .AsNoTracking()
            .OrderBy(z => z.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AccessZoneSummary>> GetAllSummariesAsync(CancellationToken cancellationToken)
    {
        return await context.AccessZones
            .AsNoTracking()
            .OrderBy(z => z.Name)
            .Select(z => new AccessZoneSummary(
                z.Id,
                z.Name,
                z.Description,
                context.Devices.Count(d => d.ZoneId == z.Id),
                context.AccessCards.Count(c => c.ZoneId == z.Id),
                z.CreatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<AccessZoneSummary?> GetSummaryByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.AccessZones
            .AsNoTracking()
            .Where(z => z.Id == id)
            .Select(z => new AccessZoneSummary(
                z.Id,
                z.Name,
                z.Description,
                context.Devices.Count(d => d.ZoneId == z.Id),
                context.AccessCards.Count(c => c.ZoneId == z.Id),
                z.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AccessZone?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.AccessZones
            .AsNoTracking()
            .FirstOrDefaultAsync(z => z.Id == id, cancellationToken);
    }

    public async Task<AccessZone?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.AccessZones.FindAsync([id], cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalized = name.ToLowerInvariant();
        return await context.AccessZones
            .AnyAsync(z => z.Name.ToLower() == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid excludeId, CancellationToken cancellationToken)
    {
        var normalized = name.ToLowerInvariant();
        return await context.AccessZones
            .AnyAsync(z => z.Name.ToLower() == normalized && z.Id != excludeId, cancellationToken);
    }

    public async Task<int> GetDeviceCountAsync(Guid zoneId, CancellationToken cancellationToken)
    {
        return await context.Devices
            .CountAsync(d => d.ZoneId == zoneId, cancellationToken);
    }

    public async Task<int> GetCardCountAsync(Guid zoneId, CancellationToken cancellationToken)
    {
        return await context.AccessCards
            .CountAsync(c => c.ZoneId == zoneId, cancellationToken);
    }

    public async Task AddAsync(AccessZone zone, CancellationToken cancellationToken)
    {
        await context.AccessZones.AddAsync(zone, cancellationToken);
    }

    public void Remove(AccessZone zone)
    {
        context.AccessZones.Remove(zone);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
