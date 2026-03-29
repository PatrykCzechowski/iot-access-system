using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Exceptions;
using MediatR;

namespace AccessControl.Application.Cards.Commands;

public sealed class CreateAccessCardCommandHandler(IAccessCardRepository repository)
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

        var card = AccessCard.Create(request.CardUid, request.ZoneId, request.Label);

        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            card.AssignUser(request.UserId);
        }

        await repository.AddAsync(card, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return card.Id;
    }
}
