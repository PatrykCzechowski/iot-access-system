using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.Cardholders.Commands;

public sealed class UpdateCardholderCommandHandler(ICardholderRepository repository)
    : IRequestHandler<UpdateCardholderCommand>
{
    public async Task Handle(UpdateCardholderCommand request, CancellationToken cancellationToken)
    {
        var cardholder = await repository.GetByIdTrackedAsync(request.Id, cancellationToken)
                         ?? throw new KeyNotFoundException($"Cardholder '{request.Id}' not found.");

        cardholder.Update(request.FirstName, request.LastName, request.Email, request.PhoneNumber);

        await repository.SaveChangesAsync(cancellationToken);
    }
}
