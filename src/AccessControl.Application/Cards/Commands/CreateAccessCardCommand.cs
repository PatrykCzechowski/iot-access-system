using MediatR;

namespace AccessControl.Application.Cards.Commands;

public record CreateAccessCardCommand(
    string CardUid,
    Guid? CardholderId,
    string? Label) : IRequest<Guid>;
