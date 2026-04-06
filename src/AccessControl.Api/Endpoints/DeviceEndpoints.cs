using AccessControl.Application.Devices.Commands;
using AccessControl.Application.Devices.DTOs;
using AccessControl.Application.Devices.Queries;
using MediatR;

namespace AccessControl.Api.Endpoints;

public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/devices")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithTags("Devices");

        // Standard CRUD
        group
            .WithGetAll<GetDevicesQuery, DeviceDto>("GetDevices")
            .WithGetById<DeviceDto>("GetDeviceById", id => new GetDeviceByIdQuery(id))
            .WithCreate<CreateDeviceCommand>("CreateDevice", "GetDeviceById")
            .WithUpdate("UpdateDevice", (Guid id, UpdateDeviceRequest body) =>
                new UpdateDeviceCommand(id, body.Name, body.ZoneId))
            .WithDelete("DeleteDevice", id => new DeleteDeviceCommand(id));

        // Device-specific actions

        group.MapPost("/scan", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ScanForDevicesCommand(), ct);
                return Results.Ok(result);
            })
            .WithName("ScanForDevices")
            .Produces<IReadOnlyCollection<DiscoveredDeviceDto>>();

        group.MapGet("/discovered", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetDiscoveredDevicesQuery(), ct);
                return Results.Ok(result);
            })
            .WithName("GetDiscoveredDevices")
            .Produces<IReadOnlyCollection<DiscoveredDeviceDto>>();

        group.MapPost("/{id:guid}/provision",
                async (Guid id, ISender sender, CancellationToken ct) =>
                {
                    var result = await sender.Send(new ProvisionDeviceCommand(id), ct);
                    return result.Success
                        ? Results.Ok(result)
                        : Results.UnprocessableEntity(result);
                })
            .WithName("ProvisionDevice")
            .Produces<DeviceProvisionResult>()
            .Produces<DeviceProvisionResult>(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/enrollment/start",
                async (Guid id, ISender sender, CancellationToken ct) =>
                {
                    await sender.Send(new StartEnrollmentCommand(id), ct);
                    return Results.Ok();
                })
            .WithName("StartEnrollment")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/enrollment/cancel",
                async (Guid id, ISender sender, CancellationToken ct) =>
                {
                    await sender.Send(new CancelEnrollmentCommand(id), ct);
                    return Results.Ok();
                })
            .WithName("CancelEnrollment")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/config",
                async (Guid id, UpdateDeviceConfigRequest body, ISender sender, CancellationToken ct) =>
                {
                    await sender.Send(new UpdateDeviceConfigCommand(id, body.Settings), ct);
                    return Results.NoContent();
                })
            .WithName("UpdateDeviceConfig")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return app;
    }

    private record UpdateDeviceRequest(string Name, Guid ZoneId);
    private record UpdateDeviceConfigRequest(Dictionary<string, string> Settings);
}
