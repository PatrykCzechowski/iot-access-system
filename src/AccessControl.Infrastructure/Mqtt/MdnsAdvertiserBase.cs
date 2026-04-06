using Makaretu.Dns;
using Makaretu.Dns.Resolving;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AccessControl.Infrastructure.Mqtt;

/// <summary>
/// Base class for mDNS service advertisers.
/// Handles MulticastService lifecycle, address record replacement, and graceful shutdown.
/// </summary>
public abstract class MdnsAdvertiserBase : BackgroundService
{
    private readonly ILogger _logger;
    private MulticastService? _mdns;
    private ServiceDiscovery? _sd;

    protected MdnsAdvertiserBase(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates the service profile to advertise, or null to skip advertisement.
    /// </summary>
    protected abstract ServiceProfile? CreateProfile();

    /// <summary>
    /// Returns a human-readable name for log messages (e.g. "MQTT broker", "API").
    /// </summary>
    protected abstract string ServiceDescription { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var profile = CreateProfile();
            if (profile is null)
            {
                _logger.LogWarning(
                    "mDNS: {Service} advertisement skipped — could not create profile",
                    ServiceDescription);
                return;
            }

            _mdns = new MulticastService();
            _sd = new ServiceDiscovery(_mdns);

            _sd.Advertise(profile);
            _mdns.Start();

            _logger.LogInformation(
                "mDNS: advertising {Service} on {ServiceType}",
                ServiceDescription, profile.FullyQualifiedName);

            await Task.Delay(Timeout.Infinite, stoppingToken)
                .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            try { _sd.Unadvertise(profile); } catch { /* library may throw if already disposed */ }
            try { _mdns.Stop(); } catch { /* ignore */ }

            _logger.LogInformation("mDNS: {Service} advertisement stopped", ServiceDescription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start mDNS advertisement for {Service}", ServiceDescription);
        }
    }

    /// <summary>
    /// Replaces auto-detected address records with a specific LAN IP in the profile.
    /// </summary>
    protected static void ReplaceAddressRecords(ServiceProfile profile, IPAddress address)
    {
        foreach (var record in profile.Resources.OfType<AddressRecord>().ToList())
        {
            profile.Resources.Remove(record);
        }

        profile.Resources.Add(new ARecord
        {
            Name = profile.HostName,
            Address = address,
            TTL = TimeSpan.FromMinutes(1)
        });
    }

    public override void Dispose()
    {
        _sd?.Dispose();
        _mdns?.Dispose();
        base.Dispose();
    }
}
