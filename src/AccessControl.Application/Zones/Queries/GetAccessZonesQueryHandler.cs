using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.Zones.DTOs;
using MediatR;

namespace AccessControl.Application.Zones.Queries;

public sealed class GetAccessZonesQueryHandler(IAccessZoneRepository repository)
    : IRequestHandler<GetAccessZonesQuery, IReadOnlyCollection<AccessZoneDto>>
{
    public async Task<IReadOnlyCollection<AccessZoneDto>> Handle(
        GetAccessZonesQuery request,
        CancellationToken cancellationToken)
    {
        var summaries = await repository.GetAllSummariesAsync(cancellationToken);

        return summaries
            .Select(s => new AccessZoneDto(
                s.Id,
                s.Name,
                s.Description,
                s.DeviceCount,
                s.ProfileCount,
                s.CreatedAt))
            .ToArray();
    }
}
