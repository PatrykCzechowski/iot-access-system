using AccessControl.Application.Common.Extensions;
using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using MediatR;

namespace AccessControl.Application.Cardholders.Commands;

public sealed class CreateCardholderCommandHandler(
    ICardholderRepository repository,
    IAccessProfileRepository profileRepository)
    : IRequestHandler<CreateCardholderCommand, Guid>
{
    public async Task<Guid> Handle(CreateCardholderCommand request, CancellationToken cancellationToken)
    {
        var cardholder = Cardholder.Create(request.FirstName, request.LastName, request.Email, request.PhoneNumber);

        if (request.AccessProfileIds is { Count: > 0 })
        {
            var profiles = await profileRepository.GetByIdsOrThrowAsync(request.AccessProfileIds, cancellationToken);
            foreach (var profile in profiles)
            {
                cardholder.AccessProfiles.Add(profile);
            }
        }

        await repository.AddAsync(cardholder, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return cardholder.Id;
    }
}
