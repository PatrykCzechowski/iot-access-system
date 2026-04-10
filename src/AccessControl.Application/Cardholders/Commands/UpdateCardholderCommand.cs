using MediatR;

namespace AccessControl.Application.Cardholders.Commands;

public record UpdateCardholderCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    List<Guid>? AccessProfileIds = null) : IRequest;
