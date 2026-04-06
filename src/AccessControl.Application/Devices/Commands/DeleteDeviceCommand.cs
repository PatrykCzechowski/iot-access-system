using MediatR;

namespace AccessControl.Application.Devices.Commands;

public record DeleteDeviceCommand(Guid Id) : IRequest;
