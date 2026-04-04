using AccessControl.Application.AccessLogs.DTOs;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.AccessLogs.Queries;

public sealed class GetAccessLogsQueryHandler(IAccessLogRepository repository)
    : IRequestHandler<GetAccessLogsQuery, PagedResult<AccessLogDto>>
{
    public async Task<PagedResult<AccessLogDto>> Handle(
        GetAccessLogsQuery request, CancellationToken cancellationToken)
    {
        var filter = new AccessLogFilter(
            request.Page,
            request.PageSize,
            request.DeviceId,
            request.ZoneId,
            request.CardUid,
            request.AccessGranted,
            request.From,
            request.To);

        var (items, totalCount) = await repository.GetPagedWithCountAsync(filter, cancellationToken);

        var dtos = items.Select(l => new AccessLogDto(
            l.Id,
            l.CardUid,
            l.DeviceId,
            l.DeviceName,
            l.ZoneId,
            l.ZoneName,
            l.UserName,
            l.AccessGranted,
            l.Message,
            l.Timestamp)).ToArray();

        return new PagedResult<AccessLogDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
