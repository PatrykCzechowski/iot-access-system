using Makaretu.Dns;
using Makaretu.Dns.Resolving;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace AccessControl.Infrastructure.Mqtt;

/// <summary>
/// Advertises the MQTT broker as _mqtt._tcp via mDNS so ESP32 devices
/// can auto-discover it without hardcoding an IP address.
/// </summary>
public sealed class MqttBrokerMdnsAdvertiser : BackgroundService
{
    private readonly MqttOptions _options;
    private readonly ILogger<MqttBrokerMdnsAdvertiser> _logger;
    private MulticastService? _mdns;
    private ServiceDiscovery? _sd;

    public MqttBrokerMdnsAdvertiser(
        IOptions<MqttOptions> options,
        ILogger<MqttBrokerMdnsAdvertiser> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _mdns = new MulticastService();
            _sd = new ServiceDiscovery(_mdns);

            var brokerIp = ResolveAdvertiseAddress();
            if (brokerIp is null)
            {
                _logger.LogWarning("Could not determine a LAN IP address to advertise MQTT broker. " +
                                   "mDNS advertisement skipped");
                return;
            }

            var port = (ushort)_options.Port;

            var profile = new ServiceProfile("mqtt-broker", "_mqtt._tcp", port);
            profile.AddProperty("server", _options.Host);

            // Replace the auto-detected addresses with the resolved one
            foreach (var record in profile.Resources.OfType<AddressRecord>().ToList())
            {
                profile.Resources.Remove(record);
            }

            profile.Resources.Add(new ARecord
            {
                Name = profile.HostName,
                Address = brokerIp,
                TTL = TimeSpan.FromMinutes(1)
            });

            _sd.Advertise(profile);
            _mdns.Start();

            _logger.LogInformation(
                "mDNS: advertising MQTT broker as _mqtt._tcp on {Ip}:{Port}",
                brokerIp, port);

            // Wait until the host is shutting down
            await Task.Delay(Timeout.Infinite, stoppingToken)
                .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            // Cleanup — runs before Dispose(), so _sd/_mdns are still valid
            try { _sd.Unadvertise(profile); } catch { /* library may throw if already disposed */ }
            try { _mdns.Stop(); } catch { /* ignore */ }

            _logger.LogInformation("mDNS: MQTT broker advertisement stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start mDNS advertisement for MQTT broker");
        }
    }

    public override void Dispose()
    {
        _sd?.Dispose();
        _mdns?.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// If the MQTT host is "localhost" or a Docker service name, resolve to
    /// the machine's actual LAN IP so ESP32 devices can reach the broker.
    /// </summary>
    private IPAddress? ResolveAdvertiseAddress()
    {
        var host = _options.Host;

        // If already a routable IP, use it directly
        if (IPAddress.TryParse(host, out var parsed) && !IPAddress.IsLoopback(parsed))
        {
            return parsed;
        }

        // Exclude virtual/VPN adapters (Hyper-V, VirtualBox, VMware, WSL, Docker, VPN)
        var physicalInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                         ni.NetworkInterfaceType is not (NetworkInterfaceType.Loopback
                             or NetworkInterfaceType.Tunnel) &&
                         !ni.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase) &&
                         !ni.Description.Contains("VMware", StringComparison.OrdinalIgnoreCase) &&
                         !ni.Description.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) &&
                         !ni.Name.Contains("vEthernet", StringComparison.OrdinalIgnoreCase) &&
                         !ni.Name.Contains("WSL", StringComparison.OrdinalIgnoreCase) &&
                         !ni.Name.Contains("docker", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Prefer Wireless over Ethernet (ESP32 devices are on WiFi)
        var ordered = physicalInterfaces
            .OrderByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
            .ThenByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet);

        return ordered
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork &&
                         !IPAddress.IsLoopback(ua.Address))
            .Select(ua => ua.Address)
            .FirstOrDefault();
    }
}
