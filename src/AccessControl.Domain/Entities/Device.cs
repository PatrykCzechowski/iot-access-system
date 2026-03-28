using AccessControl.Domain.Enums;

namespace AccessControl.Domain.Entities;

public class Device
{
    public Guid Id { get; init; }
    public Guid HardwareId { get; init; }
    public required string Name { get; init; }
    public DeviceAdapterType AdapterType { get; init; }
    public DeviceFeatures Features { get; init; }
    public Guid ZoneId { get; private set; }
    public DeviceStatus Status { get; private set; } = DeviceStatus.Offline;
    public DateTime? LastHeartbeat { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; private set; }

    public static Device Create(
        string name,
        Guid zoneId,
        Guid hardwareId,
        DeviceAdapterType adapterType,
        DeviceFeatures features)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (zoneId == Guid.Empty)
        {
            throw new ArgumentException("ZoneId cannot be empty.", nameof(zoneId));
        }

        if (hardwareId == Guid.Empty)
        {
            throw new ArgumentException("HardwareId cannot be empty.", nameof(hardwareId));
        }

        var now = DateTime.UtcNow;

        return new Device
        {
            Id = Guid.NewGuid(),
            HardwareId = hardwareId,
            Name = name.Trim(),
            AdapterType = adapterType,
            Features = features,
            ZoneId = zoneId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void RecordHeartbeat()
    {
        var now = DateTime.UtcNow;
        Status = DeviceStatus.Online;
        LastHeartbeat = now;
        UpdatedAt = now;
    }
}
