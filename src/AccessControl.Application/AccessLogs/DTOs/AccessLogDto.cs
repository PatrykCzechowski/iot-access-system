namespace AccessControl.Application.AccessLogs.DTOs;

public record AccessLogDto(
    Guid Id,
    string CardUid,
    Guid DeviceId,
    string DeviceName,
    Guid ZoneId,
    string ZoneName,
    string? UserName,
    bool AccessGranted,
    string? Message,
    DateTime Timestamp);
