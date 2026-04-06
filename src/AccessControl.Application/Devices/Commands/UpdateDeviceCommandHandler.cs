using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.Devices.Commands;

public sealed class UpdateDeviceCommandHandler(IDeviceRepository repository)
    : IRequestHandler<UpdateDeviceCommand>
{
    public async Task Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await repository.GetByIdTrackedAsync(request.Id, cancellationToken)
                     ?? throw new KeyNotFoundException($"Device '{request.Id}' not found.");

        device.Update(request.Name, request.ZoneId);

        await repository.SaveChangesAsync(cancellationToken);
    }
}
