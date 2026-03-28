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

        return app;
    }
}
