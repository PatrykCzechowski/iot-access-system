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

        // POST /api/devices/scan — trigger mDNS scan, return discovered devices
        group.MapPost("/scan", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ScanForDevicesCommand(), ct);
                return Results.Ok(result);
            })
            .WithName("ScanForDevices")
            .Produces<IReadOnlyCollection<DiscoveredDeviceDto>>();

        // GET /api/devices/discovered — return cached discovered devices (no scan)
        group.MapGet("/discovered", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetDiscoveredDevicesQuery(), ct);
                return Results.Ok(result);
            })
            .WithName("GetDiscoveredDevices")
            .Produces<IReadOnlyCollection<DiscoveredDeviceDto>>();

        // GET /api/devices — registered devices
        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetDevicesQuery(), ct);
                return Results.Ok(result);
            })
            .WithName("GetDevices")
            .Produces<IReadOnlyCollection<DeviceDto>>();

        // GET /api/devices/{id}
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetDeviceByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .WithName("GetDeviceById")
            .Produces<DeviceDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // POST /api/devices — register a discovered device by HardwareId
        group.MapPost("/", async (CreateDeviceCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return Results.Created($"/api/devices/{id}", new { id });
            })
            .WithName("CreateDevice")
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        // POST /api/devices/{id}/provision — push MQTT config to device
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

        // POST /api/devices/{id}/enrollment/start — start card enrollment mode
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

        // POST /api/devices/{id}/enrollment/cancel — cancel card enrollment mode
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

        // PUT /api/devices/{id}/config — update device dynamic configuration
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

    private record UpdateDeviceConfigRequest(Dictionary<string, string> Settings);
}
