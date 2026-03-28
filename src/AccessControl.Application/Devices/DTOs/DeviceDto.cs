using AccessControl.Domain.Enums;

namespace AccessControl.Application.Devices.DTOs;

public record DeviceDto(
    Guid Id,
    Guid HardwareId,
    string Name,
    DeviceAdapterType AdapterType,
    DeviceFeatures Features,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
