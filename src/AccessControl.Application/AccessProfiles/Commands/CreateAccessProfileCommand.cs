using MediatR;

namespace AccessControl.Application.AccessProfiles.Commands;

public record CreateAccessProfileCommand(
    string Name,
    string? Description,
    List<Guid>? ZoneIds = null) : IRequest<Guid>;
