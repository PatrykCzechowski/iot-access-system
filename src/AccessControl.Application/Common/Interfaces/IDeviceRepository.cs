using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;

namespace AccessControl.Application.Common.Interfaces;

public interface IDeviceRepository
{
    Task<IReadOnlyCollection<Device>> GetAllAsync(CancellationToken cancellationToken);
    Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Device?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task<Device?> GetByHardwareIdAsync(Guid hardwareId, CancellationToken cancellationToken);
    Task<Device?> GetByHardwareIdTrackedAsync(Guid hardwareId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Device>> GetOnlineByZoneAndFeatureAsync(Guid zoneId, DeviceFeatures feature, CancellationToken cancellationToken);
    Task<bool> ExistsByHardwareIdAsync(Guid hardwareId, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> GetExistingHardwareIdsAsync(IEnumerable<Guid> hardwareIds, CancellationToken cancellationToken);
    Task AddAsync(Device device, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
