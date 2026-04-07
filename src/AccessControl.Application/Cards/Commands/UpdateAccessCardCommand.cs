using MediatR;

namespace AccessControl.Application.Cards.Commands;

public record UpdateAccessCardCommand(
    Guid Id,
    Guid? CardholderId,
    string? Label,
    bool IsActive) : IRequest;
