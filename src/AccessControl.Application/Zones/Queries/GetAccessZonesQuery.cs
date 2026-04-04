using AccessControl.Application.Zones.DTOs;
using MediatR;

namespace AccessControl.Application.Zones.Queries;

public record GetAccessZonesQuery : IRequest<IReadOnlyCollection<AccessZoneDto>>;
