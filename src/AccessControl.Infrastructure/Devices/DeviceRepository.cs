using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Devices;

public sealed class DeviceRepository(AccessControlDbContext dbContext) : IDeviceRepository
{
    public async Task<IReadOnlyCollection<Device>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Devices
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Devices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task AddAsync(Device device, CancellationToken cancellationToken)
    {
        await dbContext.Devices.AddAsync(device, cancellationToken);
    }

    public async Task<bool> ExistsByHardwareIdAsync(Guid hardwareId, CancellationToken cancellationToken)
    {
        return await dbContext.Devices
            .AnyAsync(d => d.HardwareId == hardwareId, cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> GetExistingHardwareIdsAsync(
        IEnumerable<Guid> hardwareIds, CancellationToken cancellationToken)
    {
        var ids = hardwareIds.ToList();
        var existing = await dbContext.Devices
            .Where(d => ids.Contains(d.HardwareId))
            .Select(d => d.HardwareId)
            .ToListAsync(cancellationToken);
        return existing.ToHashSet();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
