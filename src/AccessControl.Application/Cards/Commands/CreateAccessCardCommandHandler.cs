using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Exceptions;
using MediatR;

namespace AccessControl.Application.Cards.Commands;

public sealed class CreateAccessCardCommandHandler(
    IAccessCardRepository repository,
    ICardholderRepository cardholderRepository)
    : IRequestHandler<CreateAccessCardCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccessCardCommand request, CancellationToken cancellationToken)
    {
        var normalizedUid = AccessCard.NormalizeUid(request.CardUid);

        var exists = await repository.ExistsByCardUidAsync(normalizedUid, cancellationToken);
        if (exists)
        {
            throw new BusinessRuleException($"Card with UID '{normalizedUid}' is already registered.");
        }

        var card = AccessCard.Create(request.CardUid, request.Label);

        if (request.CardholderId.HasValue)
        {
            var cardholder = await cardholderRepository.GetByIdAsync(request.CardholderId.Value, cancellationToken)
                             ?? throw new KeyNotFoundException($"Cardholder '{request.CardholderId.Value}' not found.");
            card.AssignCardholder(cardholder.Id);
        }

        await repository.AddAsync(card, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return card.Id;
    }
}
