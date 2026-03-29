using System.Collections.Frozen;
using AccessControl.Domain.Enums;
using AccessControl.Domain.Exceptions;

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
    private Dictionary<string, string> _configuration = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> Configuration => _configuration;
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
            throw new DomainValidationException("ZoneId cannot be empty.");
        }

        if (hardwareId == Guid.Empty)
        {
            throw new DomainValidationException("HardwareId cannot be empty.");
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

    private static readonly FrozenDictionary<string, Func<string, bool>> ConfigValidators =
        new Dictionary<string, Func<string, bool>>
        {
            ["lockOpenDurationSec"] = v => int.TryParse(v, out var n) && n is > 0 and <= 60,
            ["heartbeatIntervalSec"] = v => int.TryParse(v, out var n) && n is >= 5 and <= 300,
            ["enrollmentTimeoutSec"] = v => int.TryParse(v, out var n) && n is > 0 and <= 120,
            ["buzzerEnabled"] = v => bool.TryParse(v, out _),
            ["ledBrightness"] = v => int.TryParse(v, out var n) && n is >= 0 and <= 255,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public void UpdateConfiguration(Dictionary<string, string> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var invalidKeys = settings.Keys.Where(k => !ConfigValidators.ContainsKey(k)).ToList();
        if (invalidKeys.Count > 0)
        {
            throw new DomainValidationException($"Invalid configuration keys: {string.Join(", ", invalidKeys)}");
        }

        var invalidValues = settings
            .Where(kv => !ConfigValidators[kv.Key](kv.Value))
            .Select(kv => $"{kv.Key}='{kv.Value}'")
            .ToList();
        if (invalidValues.Count > 0)
        {
            throw new DomainValidationException($"Invalid configuration values: {string.Join(", ", invalidValues)}");
        }

        foreach (var (key, value) in settings)
        {
            var canonicalKey = ConfigValidators.Keys.First(k =>
                string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            _configuration[canonicalKey] = value;
        }

        UpdatedAt = DateTime.UtcNow;
    }
}
