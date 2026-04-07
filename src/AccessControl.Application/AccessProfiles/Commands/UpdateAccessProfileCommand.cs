using MediatR;

namespace AccessControl.Application.AccessProfiles.Commands;

public record UpdateAccessProfileCommand(
    Guid Id,
    string Name,
    string? Description,
    List<Guid> ZoneIds) : IRequest;
