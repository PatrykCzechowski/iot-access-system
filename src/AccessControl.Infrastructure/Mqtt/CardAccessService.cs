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

        // Publish validation result to card reader
        var resultTopic = MqttTopics.CardResult(deviceHardwareId);
        var result = JsonSerializer.Serialize(new
        {
            granted,
            uid,
            message = granted ? "Access granted" : "Access denied"
        });
        await mqttService.PublishAsync(resultTopic, result, cancellationToken: cancellationToken);

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

        return new CardAccessResult(granted, uid, granted ? "Access granted" : "Access denied");
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
}
