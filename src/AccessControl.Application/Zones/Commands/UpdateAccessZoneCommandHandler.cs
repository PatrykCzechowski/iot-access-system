using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Exceptions;
using MediatR;

namespace AccessControl.Application.Zones.Commands;

public sealed class UpdateAccessZoneCommandHandler(IAccessZoneRepository repository)
    : IRequestHandler<UpdateAccessZoneCommand>
{
    public async Task Handle(UpdateAccessZoneCommand request, CancellationToken cancellationToken)
    {
        var zone = await repository.GetByIdTrackedAsync(request.Id, cancellationToken)
                   ?? throw new KeyNotFoundException($"Access zone '{request.Id}' not found.");

        var nameExists = await repository.ExistsByNameAsync(request.Name.Trim(), request.Id, cancellationToken);
        if (nameExists)
        {
            throw new BusinessRuleException($"Zone with name '{request.Name.Trim()}' already exists.");
        }

        zone.UpdateName(request.Name);
        zone.UpdateDescription(request.Description);

        await repository.SaveChangesAsync(cancellationToken);
    }
}
