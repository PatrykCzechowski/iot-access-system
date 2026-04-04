using System.Text.Json;
using System.Text.RegularExpressions;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.Mqtt.Handlers;

public sealed partial class CardScannedMqttHandler(
    ICardAccessService cardAccessService,
    ILogger<CardScannedMqttHandler> logger) : IMqttMessageHandler
{
    public bool CanHandle(string topic) => TopicPattern().IsMatch(topic);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    public async Task HandleAsync(string topic, string payload, CancellationToken cancellationToken)
    {
        if (!MqttTopics.TryExtractHardwareId(topic, out var hardwareId))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            logger.LogWarning("Empty card scanned payload from {HardwareId}", hardwareId);
            return;
        }

        CardScannedMessage? msg;
        try
        {
            msg = JsonSerializer.Deserialize<CardScannedMessage>(payload, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Malformed JSON in card scanned payload from {HardwareId}", hardwareId);
            return;
        }

        if (msg is null || string.IsNullOrWhiteSpace(msg.Uid))
        {
            logger.LogWarning("Invalid card scanned payload from {HardwareId}", hardwareId);
            return;
        }

        await cardAccessService.ValidateAndGrantAccessAsync(hardwareId, msg.Uid, cancellationToken);
    }

    [GeneratedRegex(@"^accesscontrol/([^/]+)/card/scanned$")]
    private static partial Regex TopicPattern();

    private record CardScannedMessage(string Uid);
}
