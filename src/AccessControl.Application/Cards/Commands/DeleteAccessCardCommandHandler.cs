using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.Cards.Commands;

public sealed class DeleteAccessCardCommandHandler(IAccessCardRepository repository)
    : IRequestHandler<DeleteAccessCardCommand>
{
    public async Task Handle(DeleteAccessCardCommand request, CancellationToken cancellationToken)
    {
        var card = await repository.GetByIdTrackedAsync(request.Id, cancellationToken)
                   ?? throw new KeyNotFoundException($"Access card '{request.Id}' not found.");

        await repository.RemoveAsync(card, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
