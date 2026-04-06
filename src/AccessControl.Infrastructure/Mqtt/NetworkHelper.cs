using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace AccessControl.Infrastructure.Mqtt;

public static class NetworkHelper
{
    /// <summary>
    /// Resolves the machine's LAN IPv4 address, filtering out virtual/VPN adapters.
    /// Prefers wireless interfaces (ESP32 devices are typically on WiFi).
    /// </summary>
    public static IPAddress? GetLanAddress()
    {
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

    /// <summary>
    /// Resolves a host string to a LAN-routable IP address.
    /// If the host is already a routable IP, returns it directly.
    /// Otherwise falls back to <see cref="GetLanAddress"/>.
    /// </summary>
    public static IPAddress? ResolveAdvertiseAddress(string host)
    {
        if (IPAddress.TryParse(host, out var parsed) && !IPAddress.IsLoopback(parsed))
        {
            return parsed;
        }

        return GetLanAddress();
    }
}
