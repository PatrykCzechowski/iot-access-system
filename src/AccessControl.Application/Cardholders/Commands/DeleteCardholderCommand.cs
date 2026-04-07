using MediatR;

namespace AccessControl.Application.Cardholders.Commands;

public record DeleteCardholderCommand(Guid Id) : IRequest;
