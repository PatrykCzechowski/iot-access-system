using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.AccessProfiles.Commands;

public sealed class DeleteAccessProfileCommandHandler(IAccessProfileRepository repository)
    : IRequestHandler<DeleteAccessProfileCommand>
{
    public async Task Handle(DeleteAccessProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await repository.GetByIdTrackedAsync(request.Id, cancellationToken)
                      ?? throw new KeyNotFoundException($"Access profile '{request.Id}' not found.");

        repository.Remove(profile);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
