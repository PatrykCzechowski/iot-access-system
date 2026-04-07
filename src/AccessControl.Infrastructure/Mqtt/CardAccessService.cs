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
        var granted = await cardRepository.HasAccessToZoneAsync(uid, device.ZoneId, cancellationToken);
        var card = await cardRepository.GetByCardUidAsync(uid, cancellationToken);
        var message = granted ? "Access granted" : "Access denied";

        logger.LogInformation("Card {Uid} on device {DeviceName}: {Result}",
            uid, device.Name, granted ? "GRANTED" : "DENIED");

        // Persist audit log first — ensures we have a record even if MQTT publish fails
        try
        {
            await PersistAndNotifyAsync(device, uid, card, granted, message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist access log for card {Uid}", uid);
        }

        // Send result to the scanning device
        try
        {
            var resultTopic = MqttTopics.CardResult(deviceHardwareId);
            var resultPayload = JsonSerializer.Serialize(new { granted, uid, message });
            await mqttService.PublishAsync(resultTopic, resultPayload, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send card result to device {HardwareId} for card {Uid}",
                deviceHardwareId, uid);
        }

        // Open locks in the zone if access was granted
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

        return new CardAccessResult(granted, uid, message);
    }

    private async Task OpenZoneLocksAsync(Device device, string uid, CancellationToken cancellationToken)
    {
        // Includes the scanning device itself — combo devices (CardReaderWithLock)
        // need to receive the lock command to open their own relay.
        var lockDevices = await deviceRepository.GetOnlineByZoneAndFeatureAsync(
            device.ZoneId, DeviceFeatures.LockControl, cancellationToken);

        var lockTasks = lockDevices
            .Select(lockDevice =>
            {
                var durationSec = lockDevice.Configuration.TryGetValue("lockOpenDurationSec", out var val)
                    && int.TryParse(val, out var d) ? d : 5;
                var lockTopic = MqttTopics.LockCommand(lockDevice.HardwareId);
                var lockPayload = JsonSerializer.Serialize(new { action = "open", durationSec });
                return mqttService.PublishAsync(lockTopic, lockPayload, cancellationToken: cancellationToken);
            })
            .ToList();

        await Task.WhenAll(lockTasks);

        foreach (var lockDevice in lockDevices)
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
        var userName = card?.Cardholder?.FullName ?? card?.Label;

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
