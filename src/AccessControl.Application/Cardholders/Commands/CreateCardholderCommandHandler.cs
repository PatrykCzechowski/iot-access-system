using AccessControl.Application.Common.Interfaces;
using AccessControl.Domain.Entities;
using MediatR;

namespace AccessControl.Application.Cardholders.Commands;

public sealed class CreateCardholderCommandHandler(ICardholderRepository repository)
    : IRequestHandler<CreateCardholderCommand, Guid>
{
    public async Task<Guid> Handle(CreateCardholderCommand request, CancellationToken cancellationToken)
    {
        var cardholder = Cardholder.Create(request.FirstName, request.LastName, request.Email, request.PhoneNumber);

        await repository.AddAsync(cardholder, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return cardholder.Id;
    }
}
