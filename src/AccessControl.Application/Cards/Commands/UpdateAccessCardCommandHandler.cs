using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.Cards.Commands;

public sealed class UpdateAccessCardCommandHandler(
    IAccessCardRepository repository,
    ICardholderRepository cardholderRepository)
    : IRequestHandler<UpdateAccessCardCommand>
{
    public async Task Handle(UpdateAccessCardCommand request, CancellationToken cancellationToken)
    {
        var card = await repository.GetByIdTrackedAsync(request.Id, cancellationToken)
                   ?? throw new KeyNotFoundException($"Access card '{request.Id}' not found.");

        card.Update(request.Label, request.IsActive);

        if (request.CardholderId.HasValue)
        {
            var cardholder = await cardholderRepository.GetByIdAsync(request.CardholderId.Value, cancellationToken)
                             ?? throw new KeyNotFoundException($"Cardholder '{request.CardholderId.Value}' not found.");
            card.AssignCardholder(cardholder.Id);
        }
        else
        {
            card.UnassignCardholder();
        }

        await repository.SaveChangesAsync(cancellationToken);
    }
}
