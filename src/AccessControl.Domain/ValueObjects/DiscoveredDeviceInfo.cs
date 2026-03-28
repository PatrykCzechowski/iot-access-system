using AccessControl.Domain.Enums;

namespace AccessControl.Domain.ValueObjects;

public sealed record DiscoveredDeviceInfo(
    Guid HardwareId,
    string Model,
    string IpAddress,
    string MacAddress,
    DeviceFeatures Features,
    string FirmwareVersion,
    DateTime DiscoveredAt
);
