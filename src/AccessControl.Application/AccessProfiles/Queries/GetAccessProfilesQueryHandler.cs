using AccessControl.Application.AccessProfiles.DTOs;
using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.AccessProfiles.Queries;

public sealed class GetAccessProfilesQueryHandler(IAccessProfileRepository repository)
    : IRequestHandler<GetAccessProfilesQuery, IReadOnlyCollection<AccessProfileDto>>
{
    public async Task<IReadOnlyCollection<AccessProfileDto>> Handle(
        GetAccessProfilesQuery request,
        CancellationToken cancellationToken)
    {
        return await repository.GetAllWithDetailsAsync(cancellationToken);
    }
}
