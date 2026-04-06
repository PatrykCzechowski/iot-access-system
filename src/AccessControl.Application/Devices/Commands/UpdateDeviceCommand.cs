using MediatR;

namespace AccessControl.Application.Devices.Commands;

public record UpdateDeviceCommand(
    Guid Id,
    string Name,
    Guid ZoneId) : IRequest;
