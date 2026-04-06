using System.Numerics;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.Devices.Abstractions;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccessControl.Application.Devices.Commands;

public sealed class CreateDeviceCommandHandler(
    IDeviceRepository repository,
    IDeviceDiscoveryService discoveryService,
    IDeviceAdapterResolver adapterResolver,
    IDeviceProvisioningService provisioningService,
    ILogger<CreateDeviceCommandHandler> logger)
    : IRequestHandler<CreateDeviceCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateDeviceCommand request,
        CancellationToken cancellationToken)
    {
        var alreadyExists = await repository
            .ExistsByHardwareIdAsync(request.HardwareId, cancellationToken);
        if (alreadyExists)
        {
            throw new BusinessRuleException(
                $"Device with hardware ID '{request.HardwareId}' is already registered.");
        }

        var discovered = discoveryService.GetByHardwareId(request.HardwareId)
                         ?? throw new KeyNotFoundException(
                             $"Discovered device with hardware ID '{request.HardwareId}' was not found. Run a scan first.");

        var adapter = adapterResolver.ResolveByFeatures(discovered.Features)
            .OrderBy(a => BitOperations.PopCount((uint)a.SupportedFeatures))
            .FirstOrDefault()
            ?? throw new BusinessRuleException(
                $"No adapter supports the features '{discovered.Features}' of this device.");

        var device = Device.Create(
            request.Name,
            request.ZoneId,
            discovered.HardwareId,
            adapter.AdapterType,
            discovered.Features);

        await repository.AddAsync(device, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        // Push MQTT config to the device — failure is non-blocking (admin can retry via /provision endpoint)
        try
        {
            var result = await provisioningService.ProvisionAsync(discovered.IpAddress, cancellationToken);
            if (!result.Success)
            {
                logger.LogWarning("Auto-provisioning failed for device {DeviceId}: {Error}",
                    device.Id, result.Error);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Auto-provisioning threw for device {DeviceId}", device.Id);
        }

        return device.Id;
    }
}