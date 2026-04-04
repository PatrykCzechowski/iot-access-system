using AccessControl.Application.Zones.Commands;
using AccessControl.Application.Zones.DTOs;
using AccessControl.Application.Zones.Queries;
using MediatR;

namespace AccessControl.Api.Endpoints;

public static class ZoneEndpoints
{
    public static IEndpointRouteBuilder MapZoneEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/zones")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithTags("Zones");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetAccessZonesQuery(), ct);
                return Results.Ok(result);
            })
            .WithName("GetZones")
            .Produces<IReadOnlyCollection<AccessZoneDto>>();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetAccessZoneByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .WithName("GetZoneById")
            .Produces<AccessZoneDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreateAccessZoneCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return Results.Created($"/api/zones/{id}", new { id });
            })
            .WithName("CreateZone")
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", async (Guid id, UpdateZoneRequest body, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateAccessZoneCommand(id, body.Name, body.Description), ct);
                return Results.NoContent();
            })
            .WithName("UpdateZone")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeleteAccessZoneCommand(id), ct);
                return Results.NoContent();
            })
            .WithName("DeleteZone")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private record UpdateZoneRequest(string Name, string? Description);
}
