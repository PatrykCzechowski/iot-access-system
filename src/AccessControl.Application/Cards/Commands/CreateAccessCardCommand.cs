using MediatR;

namespace AccessControl.Application.Cards.Commands;

public record CreateAccessCardCommand(
    string CardUid,
    Guid ZoneId,
    string? UserId,
    string? Label) : IRequest<Guid>;
