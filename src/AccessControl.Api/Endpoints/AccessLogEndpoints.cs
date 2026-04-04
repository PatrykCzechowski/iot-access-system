using AccessControl.Application.AccessLogs.DTOs;
using AccessControl.Application.AccessLogs.Queries;
using AccessControl.Application.Common;
using MediatR;

namespace AccessControl.Api.Endpoints;

public static class AccessLogEndpoints
{
    public static IEndpointRouteBuilder MapAccessLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/access-logs")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithTags("AccessLogs");

        group.MapGet("/", async (
                int? page,
                int? pageSize,
                Guid? deviceId,
                Guid? zoneId,
                string? cardUid,
                bool? accessGranted,
                DateTime? from,
                DateTime? to,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetAccessLogsQuery(
                    Page: Math.Max(page ?? 1, 1),
                    PageSize: Math.Clamp(pageSize ?? 20, 1, 100),
                    DeviceId: deviceId,
                    ZoneId: zoneId,
                    CardUid: cardUid?.Trim().ToUpperInvariant(),
                    AccessGranted: accessGranted,
                    From: from,
                    To: to);

                var result = await sender.Send(query, ct);
                return Results.Ok(result);
            })
            .WithName("GetAccessLogs")
            .Produces<PagedResult<AccessLogDto>>()
            .ProducesValidationProblem();

        return app;
    }
}
