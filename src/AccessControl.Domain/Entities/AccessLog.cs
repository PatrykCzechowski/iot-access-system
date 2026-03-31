using AccessControl.Domain.Exceptions;

namespace AccessControl.Domain.Entities;

/// <summary>
/// Immutable audit log entry. DeviceName, ZoneName and UserName are denormalized snapshots
/// captured at the time of the event — they intentionally survive entity renames or deletion.
/// DeviceId and ZoneId serve as correlation keys for filtering, not as enforced foreign keys.
/// </summary>
public class AccessLog
{
    private AccessLog() { }

    public Guid Id { get; init; }
    public required string CardUid { get; init; }
    public Guid DeviceId { get; init; }
    public required string DeviceName { get; init; }
    public Guid ZoneId { get; init; }
    public required string ZoneName { get; init; }
    public string? UserName { get; init; }
    public bool AccessGranted { get; init; }
    public string? Message { get; init; }
    public DateTime Timestamp { get; init; }

    public static AccessLog Create(
        string cardUid,
        Guid deviceId,
        string deviceName,
        Guid zoneId,
        string zoneName,
        string? userName,
        bool accessGranted,
        string? message = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardUid);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneName);

        if (deviceId == Guid.Empty)
        {
            throw new DomainValidationException("DeviceId cannot be empty.");
        }

        if (zoneId == Guid.Empty)
        {
            throw new DomainValidationException("ZoneId cannot be empty.");
        }

        return new AccessLog
        {
            Id = Guid.NewGuid(),
            CardUid = AccessCard.NormalizeUid(cardUid),
            DeviceId = deviceId,
            DeviceName = deviceName.Trim(),
            ZoneId = zoneId,
            ZoneName = zoneName,
            UserName = userName,
            AccessGranted = accessGranted,
            Message = message,
            Timestamp = DateTime.UtcNow,
        };
    }
}
