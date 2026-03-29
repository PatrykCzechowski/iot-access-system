using System.Text.Json;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Enums;
using AccessControl.Domain.Exceptions;
using MediatR;

namespace AccessControl.Application.Devices.Commands;

public sealed class StartEnrollmentCommandHandler(
    IDeviceRepository repository,
    IMqttService mqttService)
    : IRequestHandler<StartEnrollmentCommand>
{
    public async Task Handle(StartEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var device = await repository.GetByIdAsync(request.DeviceId, cancellationToken)
                     ?? throw new KeyNotFoundException($"Device '{request.DeviceId}' not found.");

        if (!device.Features.HasFlag(DeviceFeatures.CardReader))
        {
            throw new BusinessRuleException("Device does not support card reading.");
        }

        if (device.Status != DeviceStatus.Online)
        {
            throw new BusinessRuleException("Device is not online.");
        }

        var timeout = device.Configuration.TryGetValue("enrollmentTimeoutSec", out var val)
                      && int.TryParse(val, out var seconds) && seconds > 0 ? seconds : 30;

        var topic = MqttTopics.EnrollmentStart(device.HardwareId);
        var payload = JsonSerializer.Serialize(new { timeout });

        await mqttService.PublishAsync(topic, payload, cancellationToken: cancellationToken);
    }
}
