using MediatR;

namespace AccessControl.Application.Cards.Commands;

public record UpdateAccessCardCommand(
    Guid Id,
    Guid ZoneId,
    string? UserId,
    string? Label,
    bool IsActive) : IRequest;
