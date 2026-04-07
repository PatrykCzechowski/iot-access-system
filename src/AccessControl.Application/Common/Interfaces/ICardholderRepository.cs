using AccessControl.Domain.Entities;

namespace AccessControl.Application.Common.Interfaces;

public interface ICardholderRepository
{
    Task<IReadOnlyCollection<CardholderSummary>> GetAllSummariesAsync(CancellationToken cancellationToken);
    Task<Cardholder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Cardholder?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Cardholder cardholder, CancellationToken cancellationToken);
    void Remove(Cardholder cardholder);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public record CardholderSummary(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    int CardCount);
