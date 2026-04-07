namespace AccessControl.Application.Zones.DTOs;

public record AccessZoneDto(
    Guid Id,
    string Name,
    string? Description,
    int DeviceCount,
    int ProfileCount,
    DateTime CreatedAt);
