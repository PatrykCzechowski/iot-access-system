using MediatR;

namespace AccessControl.Application.Zones.Commands;

public record UpdateAccessZoneCommand(
    Guid Id,
    string Name,
    string? Description) : IRequest;
