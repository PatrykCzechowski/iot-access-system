using AccessControl.Application.Cards.DTOs;
using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.Cards.Queries;

public sealed class GetAccessCardsQueryHandler(IAccessCardRepository repository)
    : IRequestHandler<GetAccessCardsQuery, IReadOnlyCollection<AccessCardDto>>
{
    public async Task<IReadOnlyCollection<AccessCardDto>> Handle(
        GetAccessCardsQuery request,
        CancellationToken cancellationToken)
    {
        var cards = await repository.GetAllAsync(cancellationToken);

        return cards.Select(c => new AccessCardDto(
            c.Id,
            c.CardUid,
            c.UserId,
            c.ZoneId,
            c.IsActive,
            c.Label,
            c.CreatedAt,
            c.UpdatedAt)).ToArray();
    }
}
