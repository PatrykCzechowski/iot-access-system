using MediatR;

namespace AccessControl.Application.Devices.Commands;

public record CreateDeviceCommand(
    Guid HardwareId,
    string Name,
    Guid ZoneId
) : IRequest<Guid>;
