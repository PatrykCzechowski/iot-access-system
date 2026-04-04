namespace AccessControl.UI.Models;

public sealed record CardEnrolledItem(
    string CardUid,
    Guid? CardId,
    Guid DeviceId,
    string DeviceName,
    Guid ZoneId,
    string ZoneName,
    bool Success,
    string Message,
    DateTime Timestamp);
