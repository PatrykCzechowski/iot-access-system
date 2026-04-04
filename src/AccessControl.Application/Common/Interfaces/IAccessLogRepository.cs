using AccessControl.Domain.Entities;

namespace AccessControl.Application.Common.Interfaces;

public interface IAccessLogRepository
{
    Task<(IReadOnlyCollection<AccessLog> Items, int TotalCount)> GetPagedWithCountAsync(AccessLogFilter filter, CancellationToken cancellationToken);
    Task AddAsync(AccessLog log, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public record AccessLogFilter(
    int Page = 1,
    int PageSize = 20,
    Guid? DeviceId = null,
    Guid? ZoneId = null,
    string? CardUid = null,
    bool? AccessGranted = null,
    DateTime? From = null,
    DateTime? To = null);
