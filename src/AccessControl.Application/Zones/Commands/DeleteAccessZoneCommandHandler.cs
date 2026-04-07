using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Exceptions;
using MediatR;

namespace AccessControl.Application.Zones.Commands;

public sealed class DeleteAccessZoneCommandHandler(IAccessZoneRepository repository)
    : IRequestHandler<DeleteAccessZoneCommand>
{
    public async Task Handle(DeleteAccessZoneCommand request, CancellationToken cancellationToken)
    {
        var zone = await repository.GetByIdTrackedAsync(request.Id, cancellationToken)
                   ?? throw new KeyNotFoundException($"Access zone '{request.Id}' not found.");

        var deviceCount = await repository.GetDeviceCountAsync(request.Id, cancellationToken);
        if (deviceCount > 0)
        {
            throw new BusinessRuleException(
                $"Cannot delete zone '{zone.Name}' because it still has {deviceCount} assigned device(s).");
        }

        var profileCount = await repository.GetProfileCountAsync(request.Id, cancellationToken);
        if (profileCount > 0)
        {
            throw new BusinessRuleException(
                $"Cannot delete zone '{zone.Name}' because it is still assigned to {profileCount} access profile(s).");
        }

        repository.Remove(zone);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
