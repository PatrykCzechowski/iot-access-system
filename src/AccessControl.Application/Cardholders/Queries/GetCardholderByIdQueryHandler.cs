using AccessControl.Application.Cardholders.DTOs;
using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.Cardholders.Queries;

public sealed class GetCardholderByIdQueryHandler(ICardholderRepository repository)
    : IRequestHandler<GetCardholderByIdQuery, CardholderDto>
{
    public async Task<CardholderDto> Handle(
        GetCardholderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var cardholder = await repository.GetByIdAsync(request.Id, cancellationToken)
                         ?? throw new KeyNotFoundException($"Cardholder '{request.Id}' not found.");

        return new CardholderDto(
            cardholder.Id,
            cardholder.FirstName,
            cardholder.LastName,
            cardholder.Email,
            cardholder.PhoneNumber,
            cardholder.Cards.Count);
    }
}
