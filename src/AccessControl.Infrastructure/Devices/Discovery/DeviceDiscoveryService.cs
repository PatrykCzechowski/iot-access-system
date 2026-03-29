using System.Collections.Concurrent;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Enums;
using AccessControl.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zeroconf;

namespace AccessControl.Infrastructure.Devices.Discovery;

public sealed class DeviceDiscoveryService(
    ILogger<DeviceDiscoveryService> logger,
    IOptions<DeviceDiscoveryOptions> options)
    : IDeviceDiscoveryService
{
    private const string ServiceType = "_accesscontrol._tcp.local.";
    private static readonly TimeSpan ScanTimeout = TimeSpan.FromSeconds(5);
    private const int MaxMqttCacheSize = 500;
    private TimeSpan MqttCacheTtl => TimeSpan.FromMinutes(options.Value.MqttCacheTtlMinutes);

    private readonly ConcurrentDictionary<Guid, DiscoveredDeviceInfo> _cache = new();
    private readonly ConcurrentDictionary<Guid, (DiscoveredDeviceInfo Info, DateTime ReceivedAt)> _mqttCache = new();

    public async Task<IReadOnlyCollection<DiscoveredDeviceInfo>> ScanAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting mDNS scan for {ServiceType}...", ServiceType);

        IReadOnlyList<IZeroconfHost> hosts;
        try
        {
            hosts = await ZeroconfResolver.ResolveAsync(
                ServiceType,
                scanTime: ScanTimeout,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "mDNS scan failed");
            return MergeResults();
        }

        var discoveredIds = new HashSet<Guid>();

        foreach (var host in hosts)
        {
            var info = ParseHost(host);
            if (info is not null)
            {
                discoveredIds.Add(info.HardwareId);
                _cache.AddOrUpdate(info.HardwareId, info, (_, _) => info);
            }
        }

        // Remove stale mDNS devices (MQTT-announced devices are kept separately)
        foreach (var key in _cache.Keys)
        {
            if (!discoveredIds.Contains(key))
            {
                _cache.TryRemove(key, out _);
            }
        }

        EvictStaleMqttEntries();

        var merged = MergeResults();
        logger.LogInformation("Scan completed. Found {Count} device(s) (mDNS: {Mdns}, MQTT: {Mqtt})",
            merged.Count, _cache.Count, _mqttCache.Count);
        return merged;
    }

    public IReadOnlyCollection<DiscoveredDeviceInfo> GetCached()
    {
        return MergeResults();
    }

    public DiscoveredDeviceInfo? GetByHardwareId(Guid hardwareId)
    {
        if (_cache.TryGetValue(hardwareId, out var info))
        {
            return info;
        }

        if (_mqttCache.TryGetValue(hardwareId, out var entry) &&
            DateTime.UtcNow - entry.ReceivedAt < MqttCacheTtl)
        {
            return entry.Info;
        }

        return null;
    }

    public void RegisterAnnouncement(DiscoveredDeviceInfo info)
    {
        _mqttCache.AddOrUpdate(info.HardwareId, (info, DateTime.UtcNow), (_, _) => (info, DateTime.UtcNow));

        if (_mqttCache.Count > MaxMqttCacheSize)
        {
            EvictStaleMqttEntries();
        }
    }

    private void EvictStaleMqttEntries()
    {
        var now = DateTime.UtcNow;
        foreach (var key in _mqttCache.Keys)
        {
            if (_mqttCache.TryGetValue(key, out var entry) && now - entry.ReceivedAt > MqttCacheTtl)
            {
                _mqttCache.TryRemove(key, out _);
            }
        }
    }

    private IReadOnlyCollection<DiscoveredDeviceInfo> MergeResults()
    {
        var merged = new Dictionary<Guid, DiscoveredDeviceInfo>();
        var now = DateTime.UtcNow;

        // MQTT-announced devices first (lower priority), skip stale
        foreach (var (key, (mqttInfo, receivedAt)) in _mqttCache)
        {
            if (now - receivedAt <= MqttCacheTtl)
            {
                merged[key] = mqttInfo;
            }
        }

        // mDNS devices override (higher priority — more detailed)
        foreach (var (key, value) in _cache)
        {
            merged[key] = value;
        }

        return merged.Values.ToArray();
    }

    private DiscoveredDeviceInfo? ParseHost(IZeroconfHost host)
    {
        try
        {
            var service = host.Services.Values.FirstOrDefault();
            if (service is null)
            {
                return null;
            }

            var properties = service.Properties
                .SelectMany(p => p)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

            if (!properties.TryGetValue("hwid", out var hwidStr) || !Guid.TryParse(hwidStr, out var hwid))
            {
                logger.LogWarning("Host {Host} missing or invalid 'hwid' TXT record", host.DisplayName);
                return null;
            }

            properties.TryGetValue("model", out var model);
            properties.TryGetValue("mac", out var mac);
            properties.TryGetValue("fw", out var fw);
            properties.TryGetValue("features", out var featuresStr);

            var features = DeviceFeatures.None;
            if (int.TryParse(featuresStr, out var featuresInt))
            {
                features = (DeviceFeatures)featuresInt;
            }

            return new DiscoveredDeviceInfo(
                HardwareId: hwid,
                Model: model ?? host.DisplayName,
                IpAddress: host.IPAddress,
                MacAddress: mac ?? string.Empty,
                Features: features,
                FirmwareVersion: fw ?? "unknown",
                DiscoveredAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse mDNS host {Host}", host.DisplayName);
            return null;
        }
    }
}
