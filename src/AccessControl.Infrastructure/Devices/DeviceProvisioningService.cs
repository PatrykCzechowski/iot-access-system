using System.Net;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.Devices.DTOs;
using AccessControl.Infrastructure.Mqtt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AccessControl.Infrastructure.Devices;

public sealed class DeviceProvisioningService(
    HttpClient httpClient,
    IOptions<MqttOptions> mqttOptions,
    ILogger<DeviceProvisioningService> logger) : IDeviceProvisioningService
{
    public async Task<DeviceProvisionResult> ProvisionAsync(
        string deviceIp,
        CancellationToken cancellationToken = default)
    {
        if (!IPAddress.TryParse(deviceIp, out var parsedIp)
            || IPAddress.IsLoopback(parsedIp)
            || parsedIp.Equals(IPAddress.Any)
            || parsedIp.Equals(IPAddress.Broadcast))
        {
            return new DeviceProvisionResult(false, "Invalid device IP address.");
        }

        var options = mqttOptions.Value;

        var brokerIp = NetworkHelper.ResolveAdvertiseAddress(options.Host);
        if (brokerIp is null)
        {
            return new DeviceProvisionResult(false, "Could not resolve broker LAN address.");
        }

        var payload = new
        {
            broker = brokerIp.ToString(),
            port = options.Port,
            username = options.Username,
            password = options.Password
        };

        // NOTE: ESP32 does not support HTTPS — credentials are sent in plaintext.
        // Provisioning should only be used on a trusted local network.
        var url = $"http://{deviceIp}/api/provision";

        logger.LogDebug("Provisioning device at {DeviceIp} over plain HTTP", deviceIp);

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            content.Headers.ContentType!.CharSet = null; // ESP32 WebServer needs plain "application/json"
            using var response = await httpClient.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Provisioned device at {DeviceIp} with broker {Broker}:{Port}",
                    deviceIp, brokerIp, options.Port);
                return new DeviceProvisionResult(true);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Device at {DeviceIp} rejected provisioning: {StatusCode} {Body}",
                deviceIp, (int)response.StatusCode, body);
            return new DeviceProvisionResult(false, $"Device returned {(int)response.StatusCode}: {body}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Provisioning request to {DeviceIp} timed out", deviceIp);
            return new DeviceProvisionResult(false, "Request timed out — device may be offline.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to reach device at {DeviceIp} for provisioning", deviceIp);
            return new DeviceProvisionResult(false, $"Could not reach device: {ex.Message}");
        }
    }
}
