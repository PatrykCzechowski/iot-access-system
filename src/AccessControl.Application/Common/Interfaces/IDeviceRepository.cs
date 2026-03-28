using AccessControl.Domain.Entities;

namespace AccessControl.Application.Common.Interfaces;

public interface IDeviceRepository
{
    Task<IReadOnlyCollection<Device>> GetAllAsync(CancellationToken cancellationToken);
    Task<Device?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByHardwareIdAsync(Guid hardwareId, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> GetExistingHardwareIdsAsync(IEnumerable<Guid> hardwareIds, CancellationToken cancellationToken);
    Task AddAsync(Device device, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
