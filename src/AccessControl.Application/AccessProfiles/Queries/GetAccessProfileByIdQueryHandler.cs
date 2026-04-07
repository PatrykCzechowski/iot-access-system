using AccessControl.Application.AccessProfiles.DTOs;
using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.AccessProfiles.Queries;

public sealed class GetAccessProfileByIdQueryHandler(IAccessProfileRepository repository)
    : IRequestHandler<GetAccessProfileByIdQuery, AccessProfileDto>
{
    public async Task<AccessProfileDto> Handle(
        GetAccessProfileByIdQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
                      ?? throw new KeyNotFoundException($"Access profile '{request.Id}' not found.");

        return profile;
    }
}
