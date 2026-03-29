using System.Text.RegularExpressions;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.Mqtt.Handlers;

public sealed partial class HeartbeatMqttHandler(
    IDeviceRepository repository,
    HeartbeatThrottler throttler,
    ILogger<HeartbeatMqttHandler> logger) : IMqttMessageHandler
{

    public bool CanHandle(string topic) => TopicPattern().IsMatch(topic);

    public async Task HandleAsync(string topic, string payload, CancellationToken cancellationToken)
    {
        if (!MqttTopics.TryExtractHardwareId(topic, out var hardwareId))
        {
            return;
        }

        if (!throttler.ShouldPersist(hardwareId))
        {
            return;
        }

        var device = await repository.GetByHardwareIdTrackedAsync(hardwareId, cancellationToken);
        if (device is null)
        {
            logger.LogDebug("Heartbeat from unregistered device {HardwareId}", hardwareId);
            return;
        }

        device.RecordHeartbeat();
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Heartbeat recorded for device {DeviceName} ({HardwareId})", device.Name, hardwareId);
    }

    [GeneratedRegex(@"^accesscontrol/([^/]+)/heartbeat$")]
    private static partial Regex TopicPattern();
}
