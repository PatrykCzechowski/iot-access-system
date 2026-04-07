using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Devices;

public sealed class AccessCardRepository(AccessControlDbContext context) : IAccessCardRepository
{
    public async Task<IReadOnlyCollection<AccessCard>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.AccessCards
            .AsNoTracking()
            .Include(c => c.Cardholder)
            .OrderByDescending(c => c.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<AccessCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.AccessCards
            .AsNoTracking()
            .Include(c => c.Cardholder)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<AccessCard?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.AccessCards.FindAsync([id], cancellationToken);
    }

    public async Task<AccessCard?> GetByCardUidAsync(string cardUid, CancellationToken cancellationToken)
    {
        return await context.AccessCards
            .AsNoTracking()
            .Include(c => c.Cardholder)
            .FirstOrDefaultAsync(c => c.CardUid == cardUid, cancellationToken);
    }

    public async Task<bool> HasAccessToZoneAsync(string cardUid, Guid zoneId, CancellationToken cancellationToken)
    {
        return await context.AccessCards
            .Where(c => c.CardUid == cardUid && c.IsActive)
            .Where(c => c.Cardholder != null)
            .SelectMany(c => c.Cardholder!.AccessProfiles)
            .SelectMany(p => p.AccessProfileZones)
            .AnyAsync(apz => apz.AccessZoneId == zoneId, cancellationToken);
    }

    public async Task<bool> ExistsByCardUidAsync(string cardUid, CancellationToken cancellationToken)
    {
        return await context.AccessCards
            .AnyAsync(c => c.CardUid == cardUid, cancellationToken);
    }

    public async Task AddAsync(AccessCard card, CancellationToken cancellationToken)
    {
        await context.AccessCards.AddAsync(card, cancellationToken);
    }

    public Task RemoveAsync(AccessCard card, CancellationToken cancellationToken)
    {
        context.AccessCards.Remove(card);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
