using System.Text.Json;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccessControl.Application.Devices.Commands;

public sealed class UpdateDeviceConfigCommandHandler(
    IDeviceRepository repository,
    IMqttService mqttService,
    ILogger<UpdateDeviceConfigCommandHandler> logger)
    : IRequestHandler<UpdateDeviceConfigCommand>
{
    public async Task Handle(UpdateDeviceConfigCommand request, CancellationToken cancellationToken)
    {
        var device = await repository.GetByIdTrackedAsync(request.DeviceId, cancellationToken)
                     ?? throw new KeyNotFoundException($"Device '{request.DeviceId}' not found.");

        device.UpdateConfiguration(request.Settings);

        var topic = MqttTopics.ConfigUpdate(device.HardwareId);
        var payload = JsonSerializer.Serialize(device.Configuration);

        await repository.SaveChangesAsync(cancellationToken);

        try
        {
            await mqttService.PublishAsync(topic, payload, retain: true, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // Config saved in DB; device will receive it on reconnect via retain flag
            logger.LogError(ex, "Failed to publish config update to device {DeviceId}", request.DeviceId);
        }
    }
}
