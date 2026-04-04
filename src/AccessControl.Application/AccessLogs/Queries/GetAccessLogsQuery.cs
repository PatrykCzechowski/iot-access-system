using AccessControl.Application.AccessLogs.DTOs;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.AccessLogs.Queries;

public record GetAccessLogsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? DeviceId = null,
    Guid? ZoneId = null,
    string? CardUid = null,
    bool? AccessGranted = null,
    DateTime? From = null,
    DateTime? To = null) : IRequest<PagedResult<AccessLogDto>>;
