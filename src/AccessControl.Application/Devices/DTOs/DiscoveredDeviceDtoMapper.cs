using AccessControl.Domain.ValueObjects;

namespace AccessControl.Application.Devices.DTOs;

public static class DiscoveredDeviceDtoMapper
{
    public static IReadOnlyCollection<DiscoveredDeviceDto> ToDto(
        this IReadOnlyCollection<DiscoveredDeviceInfo> devices,
        IReadOnlySet<Guid> registeredHardwareIds)
    {
        return devices
            .Select(d => new DiscoveredDeviceDto(
                d.HardwareId,
                d.Model,
                d.IpAddress,
                d.MacAddress,
                d.Features,
                d.FirmwareVersion,
                d.DiscoveredAt,
                registeredHardwareIds.Contains(d.HardwareId)))
            .ToArray();
    }
}
