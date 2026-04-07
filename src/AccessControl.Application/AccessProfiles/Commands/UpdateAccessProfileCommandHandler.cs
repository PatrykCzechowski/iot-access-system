using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Exceptions;
using MediatR;

namespace AccessControl.Application.AccessProfiles.Commands;

public sealed class UpdateAccessProfileCommandHandler(IAccessProfileRepository repository)
    : IRequestHandler<UpdateAccessProfileCommand>
{
    public async Task Handle(UpdateAccessProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await repository.GetByIdWithZonesTrackedAsync(request.Id, cancellationToken)
                      ?? throw new KeyNotFoundException($"Access profile '{request.Id}' not found.");

        var nameExists = await repository.ExistsByNameAsync(request.Name.Trim(), request.Id, cancellationToken);
        if (nameExists)
        {
            throw new BusinessRuleException($"Access profile with name '{request.Name.Trim()}' already exists.");
        }

        profile.Update(request.Name, request.Description);

        profile.AccessProfileZones.Clear();
        foreach (var zoneId in request.ZoneIds)
        {
            profile.AccessProfileZones.Add(new AccessProfileZone
            {
                AccessProfileId = profile.Id,
                AccessZoneId = zoneId,
            });
        }

        await repository.SaveChangesAsync(cancellationToken);
    }
}
