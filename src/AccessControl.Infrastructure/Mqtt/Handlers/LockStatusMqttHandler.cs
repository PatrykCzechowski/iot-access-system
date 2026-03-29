using System.Text.RegularExpressions;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.Mqtt.Handlers;

public sealed partial class LockStatusMqttHandler(
    ILogger<LockStatusMqttHandler> logger) : IMqttMessageHandler
{
    public bool CanHandle(string topic) => TopicPattern().IsMatch(topic);

    public Task HandleAsync(string topic, string payload, CancellationToken cancellationToken)
    {
        if (!MqttTopics.TryExtractHardwareId(topic, out var hardwareId))
        {
            return Task.CompletedTask;
        }

        logger.LogInformation("Lock status from {HardwareId}: {Payload}", hardwareId, payload);

        // TODO(v2): Update lock state on the device entity and notify UI via SignalR

        return Task.CompletedTask;
    }

    [GeneratedRegex(@"^accesscontrol/([^/]+)/lock/status$")]
    private static partial Regex TopicPattern();
}
