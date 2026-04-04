using AccessControl.Domain.Entities;

namespace AccessControl.Application.Common.Interfaces;

public interface IAccessZoneRepository
{
    Task<IReadOnlyCollection<AccessZone>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AccessZoneSummary>> GetAllSummariesAsync(CancellationToken cancellationToken);
    Task<AccessZoneSummary?> GetSummaryByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AccessZone?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AccessZone?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, Guid excludeId, CancellationToken cancellationToken);
    Task<int> GetDeviceCountAsync(Guid zoneId, CancellationToken cancellationToken);
    Task<int> GetCardCountAsync(Guid zoneId, CancellationToken cancellationToken);
    Task AddAsync(AccessZone zone, CancellationToken cancellationToken);
    void Remove(AccessZone zone);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public record AccessZoneSummary(
    Guid Id,
    string Name,
    string? Description,
    int DeviceCount,
    int CardCount,
    DateTime CreatedAt);
