using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.Cards.Commands;

public sealed class UpdateAccessCardCommandHandler(IAccessCardRepository repository)
    : IRequestHandler<UpdateAccessCardCommand>
{
    public async Task Handle(UpdateAccessCardCommand request, CancellationToken cancellationToken)
    {
        var card = await repository.GetByIdTrackedAsync(request.Id, cancellationToken)
                   ?? throw new KeyNotFoundException($"Access card '{request.Id}' not found.");

        card.Update(request.ZoneId, request.Label, request.IsActive);

        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            card.AssignUser(request.UserId);
        }
        else
        {
            card.UnassignUser();
        }

        await repository.SaveChangesAsync(cancellationToken);
    }
}
