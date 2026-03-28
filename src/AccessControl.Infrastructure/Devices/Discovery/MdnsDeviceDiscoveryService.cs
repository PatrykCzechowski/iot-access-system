using System.Collections.Concurrent;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Enums;
using AccessControl.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Zeroconf;

namespace AccessControl.Infrastructure.Devices.Discovery;

public sealed class MdnsDeviceDiscoveryService(ILogger<MdnsDeviceDiscoveryService> logger)
    : IDeviceDiscoveryService
{
    private const string ServiceType = "_accesscontrol._tcp.local.";
    private static readonly TimeSpan ScanTimeout = TimeSpan.FromSeconds(5);

    private readonly ConcurrentDictionary<Guid, DiscoveredDeviceInfo> _cache = new();

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
            return _cache.Values.ToArray();
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

        // Remove stale devices that were not found in this scan
        foreach (var key in _cache.Keys)
        {
            if (!discoveredIds.Contains(key))
            {
                _cache.TryRemove(key, out _);
            }
        }

        logger.LogInformation("mDNS scan completed. Found {Count} device(s)", _cache.Count);
        return _cache.Values.ToArray();
    }

    public IReadOnlyCollection<DiscoveredDeviceInfo> GetCached()
    {
        return _cache.Values.ToArray();
    }

    public DiscoveredDeviceInfo? GetByHardwareId(Guid hardwareId)
    {
        return _cache.TryGetValue(hardwareId, out var info) ? info : null;
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
