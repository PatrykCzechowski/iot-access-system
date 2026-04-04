using System.Text.Json;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.Mqtt;

public sealed class CardAccessService(
    IDeviceRepository deviceRepository,
    IAccessCardRepository cardRepository,
    IAccessLogRepository accessLogRepository,
    IAccessZoneRepository accessZoneRepository,
    IAccessNotificationService accessNotificationService,
    IMqttService mqttService,
    ILogger<CardAccessService> logger) : ICardAccessService
{
    public async Task<CardAccessResult> ValidateAndGrantAccessAsync(
        Guid deviceHardwareId, string cardUid, CancellationToken cancellationToken)
    {
        var device = await deviceRepository.GetByHardwareIdAsync(deviceHardwareId, cancellationToken);
        if (device is null)
        {
            logger.LogWarning("Card read from unregistered device {HardwareId}", deviceHardwareId);
            return new CardAccessResult(false, cardUid, "Unknown device");
        }

        var uid = AccessCard.NormalizeUid(cardUid);
        var card = await cardRepository.GetByCardUidAsync(uid, cancellationToken);

        var granted = card is not null && card.IsActive && card.ZoneId == device.ZoneId;
        var message = granted ? "Access granted" : "Access denied";

        if (granted)
        {
            try
            {
                await OpenZoneLocksAsync(device, uid, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send lock commands for zone {ZoneId} after granting access to card {Uid}",
                    device.ZoneId, uid);
            }
        }

        logger.LogInformation("Card {Uid} on device {DeviceName}: {Result}",
            uid, device.Name, granted ? "GRANTED" : "DENIED");

        try
        {
            await PersistAndNotifyAsync(device, uid, card, granted, message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist access log for card {Uid}", uid);
        }

        return new CardAccessResult(granted, uid, message);
    }

    private async Task OpenZoneLocksAsync(Device device, string uid, CancellationToken cancellationToken)
    {
        var lockDevices = await deviceRepository.GetOnlineByZoneAndFeatureAsync(
            device.ZoneId, DeviceFeatures.LockControl, cancellationToken);

        var locksToOpen = lockDevices
            .Where(d => d.Id != device.Id)
            .ToList();

        var lockTasks = locksToOpen
            .Select(lockDevice =>
            {
                var lockTopic = MqttTopics.LockCommand(lockDevice.HardwareId);
                var lockPayload = JsonSerializer.Serialize(new { action = "open" });
                return mqttService.PublishAsync(lockTopic, lockPayload, cancellationToken: cancellationToken);
            });

        await Task.WhenAll(lockTasks);

        foreach (var lockDevice in locksToOpen)
        {
            logger.LogInformation("Lock command sent to {DeviceName} for card {Uid}", lockDevice.Name, uid);
        }
    }

    private async Task PersistAndNotifyAsync(
        Device device, string uid, AccessCard? card, bool granted, string message,
        CancellationToken cancellationToken)
    {
        var zone = await accessZoneRepository.GetByIdAsync(device.ZoneId, cancellationToken);
        var zoneName = zone?.Name ?? "Unknown";
        var userName = card?.Label;

        var log = AccessLog.Create(uid, device.Id, device.Name, device.ZoneId, zoneName, userName, granted, message);
        await accessLogRepository.AddAsync(log, cancellationToken);
        await accessLogRepository.SaveChangesAsync(cancellationToken);

        try
        {
            var notification = new AccessLogNotification(
                uid, device.Id, device.Name, device.ZoneId, zoneName, userName, granted, message, log.Timestamp);

            await accessNotificationService.NotifyCardScannedAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send access notification for card {Uid}", uid);
        }
    }
}
