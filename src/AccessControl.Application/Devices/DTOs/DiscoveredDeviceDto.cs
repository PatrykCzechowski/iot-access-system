using AccessControl.Domain.Enums;

namespace AccessControl.Application.Devices.DTOs;

public record DiscoveredDeviceDto(
    Guid HardwareId,
    string Model,
    string IpAddress,
    string MacAddress,
    DeviceFeatures Features,
    string FirmwareVersion,
    DateTime DiscoveredAt,
    bool AlreadyRegistered
);
