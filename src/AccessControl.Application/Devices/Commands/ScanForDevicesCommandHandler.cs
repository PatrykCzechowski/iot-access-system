using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.Devices.DTOs;
using MediatR;

namespace AccessControl.Application.Devices.Commands;

public sealed class ScanForDevicesCommandHandler(
    IDeviceDiscoveryService discoveryService,
    IDeviceRepository deviceRepository)
    : IRequestHandler<ScanForDevicesCommand, IReadOnlyCollection<DiscoveredDeviceDto>>
{
    public async Task<IReadOnlyCollection<DiscoveredDeviceDto>> Handle(
        ScanForDevicesCommand request,
        CancellationToken cancellationToken)
    {
        var discovered = await discoveryService.ScanAsync(cancellationToken);

        var registeredIds = await deviceRepository
            .GetExistingHardwareIdsAsync(discovered.Select(d => d.HardwareId), cancellationToken);

        return discovered.ToDto(registeredIds);
    }
}
