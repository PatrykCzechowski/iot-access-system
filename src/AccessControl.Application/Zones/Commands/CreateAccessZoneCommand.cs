using MediatR;

namespace AccessControl.Application.Zones.Commands;

public record CreateAccessZoneCommand(
    string Name,
    string? Description) : IRequest<Guid>;
