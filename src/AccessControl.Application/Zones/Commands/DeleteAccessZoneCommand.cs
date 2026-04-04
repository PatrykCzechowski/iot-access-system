using MediatR;

namespace AccessControl.Application.Zones.Commands;

public record DeleteAccessZoneCommand(Guid Id) : IRequest;
