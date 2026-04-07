using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.Zones.DTOs;
using MediatR;

namespace AccessControl.Application.Zones.Queries;

public sealed class GetAccessZoneByIdQueryHandler(IAccessZoneRepository repository)
    : IRequestHandler<GetAccessZoneByIdQuery, AccessZoneDto>
{
    public async Task<AccessZoneDto> Handle(
        GetAccessZoneByIdQuery request,
        CancellationToken cancellationToken)
    {
        var summary = await repository.GetSummaryByIdAsync(request.Id, cancellationToken)
                      ?? throw new KeyNotFoundException($"Access zone '{request.Id}' not found.");

        return new AccessZoneDto(
            summary.Id,
            summary.Name,
            summary.Description,
            summary.DeviceCount,
            summary.ProfileCount,
            summary.CreatedAt);
    }
}
