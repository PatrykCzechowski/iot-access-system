using AccessControl.Application.Common.Extensions;
using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.Cardholders.Commands;

public sealed class UpdateCardholderCommandHandler(
    ICardholderRepository repository,
    IAccessProfileRepository profileRepository)
    : IRequestHandler<UpdateCardholderCommand>
{
    public async Task Handle(UpdateCardholderCommand request, CancellationToken cancellationToken)
    {
        var cardholder = await repository.GetByIdWithProfilesTrackedAsync(request.Id, cancellationToken)
                         ?? throw new KeyNotFoundException($"Cardholder '{request.Id}' not found.");

        cardholder.Update(request.FirstName, request.LastName, request.Email, request.PhoneNumber);

        if (request.AccessProfileIds is not null)
        {
            cardholder.AccessProfiles.Clear();

            if (request.AccessProfileIds.Count > 0)
            {
                var profiles = await profileRepository.GetByIdsOrThrowAsync(request.AccessProfileIds, cancellationToken);
                foreach (var profile in profiles)
                {
                    cardholder.AccessProfiles.Add(profile);
                }
            }
        }

        await repository.SaveChangesAsync(cancellationToken);
    }
}
