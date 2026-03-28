using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.Devices.DTOs;
using MediatR;

namespace AccessControl.Application.Devices.Queries;

public sealed class GetDiscoveredDevicesQueryHandler(
    IDeviceDiscoveryService discoveryService,
    IDeviceRepository deviceRepository)
    : IRequestHandler<GetDiscoveredDevicesQuery, IReadOnlyCollection<DiscoveredDeviceDto>>
{
    public async Task<IReadOnlyCollection<DiscoveredDeviceDto>> Handle(
        GetDiscoveredDevicesQuery request,
        CancellationToken cancellationToken)
    {
        var cached = discoveryService.GetCached();

        var registeredIds = await deviceRepository
            .GetExistingHardwareIdsAsync(cached.Select(d => d.HardwareId), cancellationToken);

        return cached.ToDto(registeredIds);
    }
}
