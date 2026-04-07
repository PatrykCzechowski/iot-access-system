using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.Mqtt;

public sealed class CardEnrollmentService(
    IDeviceRepository deviceRepository,
    IAccessCardRepository cardRepository,
    IAccessZoneRepository zoneRepository,
    IAccessNotificationService notificationService,
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

        var zone = await zoneRepository.GetByIdAsync(device.ZoneId, cancellationToken);
        var zoneName = zone?.Name ?? "Unknown";

        var uid = AccessCard.NormalizeUid(cardUid);

        var exists = await cardRepository.ExistsByCardUidAsync(uid, cancellationToken);
        if (exists)
        {
            logger.LogWarning("Enrollment rejected: card {Uid} already registered", uid);

            await SafeNotifyAsync(new CardEnrolledNotification(
                uid, null, device.Id, device.Name, device.ZoneId, zoneName,
                false, "Card already registered", DateTime.UtcNow), cancellationToken);

            return new CardEnrollmentResult(false, uid, "Card already registered");
        }

        var card = AccessCard.Create(uid, $"Enrolled via {device.Name}");
        await cardRepository.AddAsync(card, cancellationToken);
        await cardRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Card {Uid} enrolled via device {DeviceName}. Assign a cardholder to grant access", uid, device.Name);

        await SafeNotifyAsync(new CardEnrolledNotification(
            uid, card.Id, device.Id, device.Name, device.ZoneId, zoneName,
            true, "Card enrolled successfully", DateTime.UtcNow), cancellationToken);

        return new CardEnrollmentResult(true, uid, "Card enrolled successfully", card.Id);
    }

    private async Task SafeNotifyAsync(CardEnrolledNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await notificationService.NotifyCardEnrolledAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send enrollment notification for card {Uid}", notification.CardUid);
        }
    }
}
