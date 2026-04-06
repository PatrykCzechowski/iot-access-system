using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.Devices.Commands;

public sealed class DeleteDeviceCommandHandler(IDeviceRepository repository)
    : IRequestHandler<DeleteDeviceCommand>
{
    public async Task Handle(DeleteDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = await repository.GetByIdTrackedAsync(request.Id, cancellationToken)
                     ?? throw new KeyNotFoundException($"Device '{request.Id}' not found.");

        await repository.DeleteAsync(device, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
