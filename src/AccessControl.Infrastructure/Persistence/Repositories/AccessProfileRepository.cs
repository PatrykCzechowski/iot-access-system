using AccessControl.Application.AccessProfiles.DTOs;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Persistence.Repositories;

public sealed class AccessProfileRepository(AccessControlDbContext context) : IAccessProfileRepository
{
    public async Task<IReadOnlyCollection<AccessProfileDto>> GetAllWithDetailsAsync(CancellationToken cancellationToken)
    {
        return await context.AccessProfiles
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new AccessProfileDto(
                p.Id,
                p.Name,
                p.Description,
                p.Cardholders.Count,
                p.AccessProfileZones.Select(apz => apz.AccessZoneId).ToArray()))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<AccessProfileDto?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.AccessProfiles
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new AccessProfileDto(
                p.Id,
                p.Name,
                p.Description,
                p.Cardholders.Count,
                p.AccessProfileZones.Select(apz => apz.AccessZoneId).ToArray()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AccessProfile?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.AccessProfiles.FindAsync([id], cancellationToken);
    }

    public async Task<AccessProfile?> GetByIdWithZonesTrackedAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.AccessProfiles
            .Include(p => p.AccessProfileZones)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalized = name.ToLowerInvariant();
        return await context.AccessProfiles
            .AnyAsync(p => p.Name.ToLower() == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid excludeId, CancellationToken cancellationToken)
    {
        var normalized = name.ToLowerInvariant();
        return await context.AccessProfiles
            .AnyAsync(p => p.Name.ToLower() == normalized && p.Id != excludeId, cancellationToken);
    }

    public async Task AddAsync(AccessProfile profile, CancellationToken cancellationToken)
    {
        await context.AccessProfiles.AddAsync(profile, cancellationToken);
    }

    public void Remove(AccessProfile profile)
    {
        context.AccessProfiles.Remove(profile);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
