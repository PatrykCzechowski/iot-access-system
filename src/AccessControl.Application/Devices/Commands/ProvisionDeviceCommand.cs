using AccessControl.Application.Devices.DTOs;
using MediatR;

namespace AccessControl.Application.Devices.Commands;

public record ProvisionDeviceCommand(Guid DeviceId) : IRequest<DeviceProvisionResult>;
