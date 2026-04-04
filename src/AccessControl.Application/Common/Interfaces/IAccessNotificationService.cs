namespace AccessControl.Application.Common.Interfaces;

public interface IAccessNotificationService
{
    Task NotifyCardScannedAsync(AccessLogNotification notification, CancellationToken cancellationToken);
    Task NotifyCardEnrolledAsync(CardEnrolledNotification notification, CancellationToken cancellationToken);
}

public record AccessLogNotification(
    string CardUid,
    Guid DeviceId,
    string DeviceName,
    Guid ZoneId,
    string ZoneName,
    string? UserName,
    bool AccessGranted,
    string? Message,
    DateTime Timestamp);

public record CardEnrolledNotification(
    string CardUid,
    Guid? CardId,
    Guid DeviceId,
    string DeviceName,
    Guid ZoneId,
    string ZoneName,
    bool Success,
    string Message,
    DateTime Timestamp);
