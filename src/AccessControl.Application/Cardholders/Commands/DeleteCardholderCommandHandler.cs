using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.Cardholders.Commands;

public sealed class DeleteCardholderCommandHandler(ICardholderRepository repository)
    : IRequestHandler<DeleteCardholderCommand>
{
    public async Task Handle(DeleteCardholderCommand request, CancellationToken cancellationToken)
    {
        var cardholder = await repository.GetByIdTrackedAsync(request.Id, cancellationToken)
                         ?? throw new KeyNotFoundException($"Cardholder '{request.Id}' not found.");

        repository.Remove(cardholder);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
