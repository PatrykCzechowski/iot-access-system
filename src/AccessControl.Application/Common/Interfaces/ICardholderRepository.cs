using AccessControl.Domain.Entities;

namespace AccessControl.Application.Common.Interfaces;

public interface ICardholderRepository
{
    Task<IReadOnlyCollection<CardholderSummary>> GetAllSummariesAsync(CancellationToken cancellationToken);
    Task<Cardholder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Cardholder?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task<Cardholder?> GetByIdWithProfilesTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Cardholder cardholder, CancellationToken cancellationToken);
    void Remove(Cardholder cardholder);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

