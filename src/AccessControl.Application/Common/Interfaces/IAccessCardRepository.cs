using AccessControl.Domain.Entities;

namespace AccessControl.Application.Common.Interfaces;

public interface IAccessCardRepository
{
    Task<IReadOnlyCollection<AccessCard>> GetAllAsync(CancellationToken cancellationToken);
    Task<AccessCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AccessCard?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task<AccessCard?> GetByCardUidAsync(string cardUid, CancellationToken cancellationToken);
    Task<bool> HasAccessToZoneAsync(string cardUid, Guid zoneId, CancellationToken cancellationToken);
    Task<bool> ExistsByCardUidAsync(string cardUid, CancellationToken cancellationToken);
    Task AddAsync(AccessCard card, CancellationToken cancellationToken);
    Task RemoveAsync(AccessCard card, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
