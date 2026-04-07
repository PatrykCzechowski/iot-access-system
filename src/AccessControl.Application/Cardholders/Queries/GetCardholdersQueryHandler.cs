using AccessControl.Application.Cardholders.DTOs;
using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.Cardholders.Queries;

public sealed class GetCardholdersQueryHandler(ICardholderRepository repository)
    : IRequestHandler<GetCardholdersQuery, IReadOnlyCollection<CardholderDto>>
{
    public async Task<IReadOnlyCollection<CardholderDto>> Handle(
        GetCardholdersQuery request,
        CancellationToken cancellationToken)
    {
        var summaries = await repository.GetAllSummariesAsync(cancellationToken);

        return summaries.Select(s => new CardholderDto(
            s.Id,
            s.FirstName,
            s.LastName,
            s.Email,
            s.PhoneNumber,
            s.CardCount)).ToArray();
    }
}
