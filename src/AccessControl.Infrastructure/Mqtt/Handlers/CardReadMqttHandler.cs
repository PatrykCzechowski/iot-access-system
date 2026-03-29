using System.Text.Json;
using System.Text.RegularExpressions;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.Mqtt.Handlers;

public sealed partial class CardReadMqttHandler(
    ICardAccessService cardAccessService,
    ILogger<CardReadMqttHandler> logger) : IMqttMessageHandler
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
            logger.LogWarning("Empty card read payload from {HardwareId}", hardwareId);
            return;
        }

        CardReadMessage? msg;
        try
        {
            msg = JsonSerializer.Deserialize<CardReadMessage>(payload);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Malformed JSON in card read payload from {HardwareId}", hardwareId);
            return;
        }

        if (msg is null || string.IsNullOrWhiteSpace(msg.Uid))
        {
            logger.LogWarning("Invalid card read payload from {HardwareId}", hardwareId);
            return;
        }

        await cardAccessService.ValidateAndGrantAccessAsync(hardwareId, msg.Uid, cancellationToken);
    }

    [GeneratedRegex(@"^accesscontrol/([^/]+)/card/read$")]
    private static partial Regex TopicPattern();

    private record CardReadMessage(string Uid);
}
