using System.Text.Json;
using System.Text.RegularExpressions;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Enums;
using AccessControl.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.Mqtt.Handlers;

public sealed partial class AnnounceMqttHandler(
    IDeviceDiscoveryService discoveryService,
    ILogger<AnnounceMqttHandler> logger) : IMqttMessageHandler
{
    private static readonly int AllKnownFeatures = (int)Enum.GetValues<DeviceFeatures>()
        .Aggregate(DeviceFeatures.None, (acc, flag) => acc | flag);

    public bool CanHandle(string topic) => TopicPattern().IsMatch(topic);

    public Task HandleAsync(string topic, string payload, CancellationToken cancellationToken)
    {
        if (!MqttTopics.TryExtractHardwareId(topic, out var hardwareId))
        {
            return Task.CompletedTask;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var rawFeatures = root.TryGetProperty("features", out var f) ? f.GetInt32() : 0;

            if ((rawFeatures & ~AllKnownFeatures) != 0)
            {
                logger.LogWarning(
                    "Device {HardwareId} announced unknown feature bits 0x{Raw:X}, stripping to known flags",
                    hardwareId, rawFeatures);
                rawFeatures &= AllKnownFeatures;
            }

            var info = new DiscoveredDeviceInfo(
                HardwareId: hardwareId,
                Model: root.TryGetProperty("model", out var m) ? m.GetString() ?? "Unknown" : "Unknown",
                IpAddress: root.TryGetProperty("ip", out var ip) ? ip.GetString() ?? "" : "",
                MacAddress: root.TryGetProperty("mac", out var mac) ? mac.GetString() ?? "" : "",
                Features: (DeviceFeatures)rawFeatures,
                FirmwareVersion: root.TryGetProperty("fw", out var fw) ? fw.GetString() ?? "unknown" : "unknown",
                DiscoveredAt: DateTime.UtcNow);

            discoveryService.RegisterAnnouncement(info);

            logger.LogInformation(
                "Device announced via MQTT: {Model} ({HardwareId}) at {Ip}",
                info.Model, hardwareId, info.IpAddress);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse announce payload from {HardwareId}", hardwareId);
        }

        return Task.CompletedTask;
    }

    [GeneratedRegex(@"^accesscontrol/([^/]+)/announce$")]
    private static partial Regex TopicPattern();
}
