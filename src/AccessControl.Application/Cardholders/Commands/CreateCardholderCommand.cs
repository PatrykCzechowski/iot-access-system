using MediatR;

namespace AccessControl.Application.Cardholders.Commands;

public record CreateCardholderCommand(
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    List<Guid>? AccessProfileIds = null) : IRequest<Guid>;
