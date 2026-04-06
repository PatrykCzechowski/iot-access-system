using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.Devices.DTOs;
using MediatR;

namespace AccessControl.Application.Devices.Commands;

public sealed class ProvisionDeviceCommandHandler(
    IDeviceRepository deviceRepository,
    IDeviceDiscoveryService discoveryService,
    IDeviceProvisioningService provisioningService)
    : IRequestHandler<ProvisionDeviceCommand, DeviceProvisionResult>
{
    public async Task<DeviceProvisionResult> Handle(
        ProvisionDeviceCommand request,
        CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken)
                     ?? throw new KeyNotFoundException(
                         $"Device '{request.DeviceId}' not found.");

        var discovered = discoveryService.GetByHardwareId(device.HardwareId)
                         ?? throw new KeyNotFoundException(
                             $"Device '{device.Name}' is not visible on the network. Run a scan first.");

        return await provisioningService.ProvisionAsync(discovered.IpAddress, cancellationToken);
    }
}
