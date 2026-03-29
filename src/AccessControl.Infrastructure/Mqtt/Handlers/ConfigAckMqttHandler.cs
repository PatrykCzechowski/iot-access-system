using System.Text.RegularExpressions;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.Mqtt.Handlers;

public sealed partial class ConfigAckMqttHandler(
    ILogger<ConfigAckMqttHandler> logger) : IMqttMessageHandler
{
    public bool CanHandle(string topic) => TopicPattern().IsMatch(topic);

    public Task HandleAsync(string topic, string payload, CancellationToken cancellationToken)
    {
        if (!MqttTopics.TryExtractHardwareId(topic, out var hardwareId))
        {
            return Task.CompletedTask;
        }

        logger.LogInformation("Config ACK received from {HardwareId}: {Payload}", hardwareId, payload);

        // TODO(v2): Persist config acknowledgment status on the device entity

        return Task.CompletedTask;
    }

    [GeneratedRegex(@"^accesscontrol/([^/]+)/config/ack$")]
    private static partial Regex TopicPattern();
}
