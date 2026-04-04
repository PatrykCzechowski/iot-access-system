using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Persistence.Repositories;

public sealed class AccessLogRepository(AccessControlDbContext context) : IAccessLogRepository
{
    public async Task<(IReadOnlyCollection<AccessLog> Items, int TotalCount)> GetPagedWithCountAsync(
        AccessLogFilter filter, CancellationToken cancellationToken)
    {
        var query = ApplyFilter(filter);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToArrayAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(AccessLog log, CancellationToken cancellationToken)
    {
        await context.AccessLogs.AddAsync(log, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<AccessLog> ApplyFilter(AccessLogFilter filter)
    {
        var query = context.AccessLogs.AsNoTracking();

        if (filter.DeviceId.HasValue)
        {
            query = query.Where(l => l.DeviceId == filter.DeviceId.Value);
        }

        if (filter.ZoneId.HasValue)
        {
            query = query.Where(l => l.ZoneId == filter.ZoneId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.CardUid))
        {
            query = query.Where(l => l.CardUid == filter.CardUid);
        }

        if (filter.AccessGranted.HasValue)
        {
            query = query.Where(l => l.AccessGranted == filter.AccessGranted.Value);
        }

        if (filter.From.HasValue)
        {
            query = query.Where(l => l.Timestamp >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(l => l.Timestamp <= filter.To.Value);
        }

        return query;
    }
}
