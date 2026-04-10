using AccessControl.Application.AccessProfiles.DTOs;
using AccessControl.Domain.Entities;

namespace AccessControl.Application.Common.Interfaces;

public interface IAccessProfileRepository
{
    Task<IReadOnlyCollection<AccessProfileDto>> GetAllWithDetailsAsync(CancellationToken cancellationToken);
    Task<AccessProfileDto?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<AccessProfile?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task<List<AccessProfile>> GetByIdsTrackedAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<AccessProfile?> GetByIdWithZonesTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, Guid excludeId, CancellationToken cancellationToken);
    Task AddAsync(AccessProfile profile, CancellationToken cancellationToken);
    void Remove(AccessProfile profile);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
