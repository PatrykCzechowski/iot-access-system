using System.Numerics;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.Devices.Abstractions;
using AccessControl.Domain.Entities;
using MediatR;

namespace AccessControl.Application.Devices.Commands;

public sealed class CreateDeviceCommandHandler(
    IDeviceRepository repository,
    IDeviceDiscoveryService discoveryService,
    IDeviceAdapterResolver adapterResolver)
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
            throw new InvalidOperationException(
                $"Device with hardware ID '{request.HardwareId}' is already registered.");
        }

        var discovered = discoveryService.GetByHardwareId(request.HardwareId)
                         ?? throw new KeyNotFoundException(
                             $"Discovered device with hardware ID '{request.HardwareId}' was not found. Run a scan first.");

        var adapter = adapterResolver.ResolveByFeatures(discovered.Features)
            .OrderBy(a => BitOperations.PopCount((uint)a.SupportedFeatures))
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"No adapter supports the features '{discovered.Features}' of this device.");

        var device = Device.Create(
            request.Name,
            request.ZoneId,
            discovered.HardwareId,
            adapter.AdapterType,
            discovered.Features);

        await repository.AddAsync(device, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return device.Id;
    }
}