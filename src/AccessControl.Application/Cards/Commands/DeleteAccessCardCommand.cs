using MediatR;

namespace AccessControl.Application.Cards.Commands;

public record DeleteAccessCardCommand(Guid Id) : IRequest;
