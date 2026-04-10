using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Persistence.Repositories;

public sealed class CardholderRepository(AccessControlDbContext context) : ICardholderRepository
{
    public async Task<IReadOnlyCollection<CardholderSummary>> GetAllSummariesAsync(CancellationToken cancellationToken)
    {
        return await context.Cardholders
            .AsNoTracking()
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Select(c => new CardholderSummary(
                c.Id,
                c.FirstName,
                c.LastName,
                c.Email,
                c.PhoneNumber,
                c.Cards.Count,
                c.AccessProfiles.Select(p => p.Id).ToArray()))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Cardholder?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Cardholders
            .AsNoTracking()
            .Include(c => c.Cards)
            .Include(c => c.AccessProfiles)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Cardholder?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Cardholders.FindAsync([id], cancellationToken);
    }

    public async Task<Cardholder?> GetByIdWithProfilesTrackedAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Cardholders
            .Include(c => c.AccessProfiles)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(Cardholder cardholder, CancellationToken cancellationToken)
    {
        await context.Cardholders.AddAsync(cardholder, cancellationToken);
    }

    public void Remove(Cardholder cardholder)
    {
        context.Cardholders.Remove(cardholder);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
