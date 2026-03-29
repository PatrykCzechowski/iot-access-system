using System.Text.Json;
using System.Text.RegularExpressions;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.Mqtt.Handlers;

public sealed partial class CardEnrolledMqttHandler(
    ICardEnrollmentService enrollmentService,
    ILogger<CardEnrolledMqttHandler> logger) : IMqttMessageHandler
{
    public bool CanHandle(string topic) => TopicPattern().IsMatch(topic);

    public async Task HandleAsync(string topic, string payload, CancellationToken cancellationToken)
    {
        if (!MqttTopics.TryExtractHardwareId(topic, out var hardwareId))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            logger.LogWarning("Empty enrollment payload from {HardwareId}", hardwareId);
            return;
        }

        CardEnrolledMessage? msg;
        try
        {
            msg = JsonSerializer.Deserialize<CardEnrolledMessage>(payload);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Malformed JSON in enrollment payload from {HardwareId}", hardwareId);
            return;
        }

        if (msg is null || string.IsNullOrWhiteSpace(msg.Uid))
        {
            logger.LogWarning("Invalid enrollment payload from {HardwareId}", hardwareId);
            return;
        }

        await enrollmentService.EnrollCardAsync(hardwareId, msg.Uid, cancellationToken);
    }

    [GeneratedRegex(@"^accesscontrol/([^/]+)/card/enrolled$")]
    private static partial Regex TopicPattern();

    private record CardEnrolledMessage(string Uid);
}
