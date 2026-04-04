using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Exceptions;
using MediatR;

namespace AccessControl.Application.Zones.Commands;

public sealed class CreateAccessZoneCommandHandler(IAccessZoneRepository repository)
    : IRequestHandler<CreateAccessZoneCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccessZoneCommand request, CancellationToken cancellationToken)
    {
        var exists = await repository.ExistsByNameAsync(request.Name.Trim(), cancellationToken);
        if (exists)
        {
            throw new BusinessRuleException($"Zone with name '{request.Name.Trim()}' already exists.");
        }

        var zone = AccessZone.Create(request.Name, request.Description);

        await repository.AddAsync(zone, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return zone.Id;
    }
}
