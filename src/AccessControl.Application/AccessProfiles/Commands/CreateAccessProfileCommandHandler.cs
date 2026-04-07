using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Exceptions;
using MediatR;

namespace AccessControl.Application.AccessProfiles.Commands;

public sealed class CreateAccessProfileCommandHandler(IAccessProfileRepository repository)
    : IRequestHandler<CreateAccessProfileCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccessProfileCommand request, CancellationToken cancellationToken)
    {
        var exists = await repository.ExistsByNameAsync(request.Name.Trim(), cancellationToken);
        if (exists)
        {
            throw new BusinessRuleException($"Access profile with name '{request.Name.Trim()}' already exists.");
        }

        var profile = AccessProfile.Create(request.Name, request.Description);

        foreach (var zoneId in request.ZoneIds)
        {
            profile.AccessProfileZones.Add(new AccessProfileZone
            {
                AccessProfileId = profile.Id,
                AccessZoneId = zoneId,
            });
        }

        await repository.AddAsync(profile, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return profile.Id;
    }
}
