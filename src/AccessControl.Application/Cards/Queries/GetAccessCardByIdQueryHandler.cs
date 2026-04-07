using AccessControl.Application.Cards.DTOs;
using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.Cards.Queries;

public sealed class GetAccessCardByIdQueryHandler(IAccessCardRepository repository)
    : IRequestHandler<GetAccessCardByIdQuery, AccessCardDto>
{
    public async Task<AccessCardDto> Handle(
        GetAccessCardByIdQuery request,
        CancellationToken cancellationToken)
    {
        var card = await repository.GetByIdAsync(request.Id, cancellationToken)
                   ?? throw new KeyNotFoundException($"Access card '{request.Id}' not found.");

        return new AccessCardDto(
            card.Id,
            card.CardUid,
            card.CardholderId,
            card.Cardholder?.FullName,
            card.IsActive,
            card.Label,
            card.CreatedAt,
            card.UpdatedAt);
    }
}
