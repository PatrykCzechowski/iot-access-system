namespace AccessControl.UI.Models;

public record AccessLogItem(
    string CardUid,
    Guid DeviceId,
    string DeviceName,
    Guid ZoneId,
    string ZoneName,
    string? UserName,
    bool AccessGranted,
    string? Message,
    DateTime Timestamp);

public record AccessLogPagedResult(
    IReadOnlyCollection<AccessLogItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
