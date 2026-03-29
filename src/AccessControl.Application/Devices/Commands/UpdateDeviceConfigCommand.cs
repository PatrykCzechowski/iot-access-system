using MediatR;

namespace AccessControl.Application.Devices.Commands;

public record UpdateDeviceConfigCommand(
    Guid DeviceId,
    Dictionary<string, string> Settings) : IRequest;
