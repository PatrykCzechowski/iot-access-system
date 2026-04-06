using AccessControl.Application.Zones.Commands;
using AccessControl.Application.Zones.DTOs;
using AccessControl.Application.Zones.Queries;

namespace AccessControl.Api.Endpoints;

public static class ZoneEndpoints
{
    public static IEndpointRouteBuilder MapZoneEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/zones")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithTags("Zones")
            .WithGetAll<GetAccessZonesQuery, AccessZoneDto>("GetZones")
            .WithGetById<AccessZoneDto>("GetZoneById", id => new GetAccessZoneByIdQuery(id))
            .WithCreate<CreateAccessZoneCommand>("CreateZone", "GetZoneById")
            .WithUpdate("UpdateZone", (Guid id, UpdateZoneRequest body) =>
                new UpdateAccessZoneCommand(id, body.Name, body.Description))
            .WithDelete("DeleteZone", id => new DeleteAccessZoneCommand(id));

        return app;
    }

    private record UpdateZoneRequest(string Name, string? Description);
}
