using System.Text.Json;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.Mqtt;

public sealed class CardEnrollmentService(
    IDeviceRepository deviceRepository,
    IAccessCardRepository cardRepository,
    IMqttService mqttService,
    ILogger<CardEnrollmentService> logger) : ICardEnrollmentService
{
    public async Task<CardEnrollmentResult> EnrollCardAsync(
        Guid deviceHardwareId, string cardUid, CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByHardwareIdAsync(deviceHardwareId, cancellationToken);
        if (device is null)
        {
            logger.LogWarning("Card enrollment from unregistered device {HardwareId}", deviceHardwareId);
            return new CardEnrollmentResult(false, cardUid, "Unknown device");
        }

        var uid = AccessCard.NormalizeUid(cardUid);
        var resultTopic = MqttTopics.EnrollmentResult(deviceHardwareId);

        var exists = await cardRepository.ExistsByCardUidAsync(uid, cancellationToken);
        if (exists)
        {
            var errorPayload = JsonSerializer.Serialize(new { success = false, cardUid = uid, message = "Card already registered" });
            await mqttService.PublishAsync(resultTopic, errorPayload, cancellationToken: cancellationToken);
            logger.LogWarning("Enrollment rejected: card {Uid} already registered", uid);
            return new CardEnrollmentResult(false, uid, "Card already registered");
        }

        var card = AccessCard.Create(uid, device.ZoneId, $"Enrolled via {device.Name}");
        await cardRepository.AddAsync(card, cancellationToken);

        var successPayload = JsonSerializer.Serialize(new { success = true, cardUid = uid, cardId = card.Id });

        await cardRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await mqttService.PublishAsync(resultTopic, successPayload, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish enrollment result for card {Uid} to device {HardwareId}", uid, deviceHardwareId);
        }

        logger.LogInformation("Card {Uid} enrolled via device {DeviceName}", uid, device.Name);
        return new CardEnrollmentResult(true, uid, "Card enrolled successfully", card.Id);
    }
}
