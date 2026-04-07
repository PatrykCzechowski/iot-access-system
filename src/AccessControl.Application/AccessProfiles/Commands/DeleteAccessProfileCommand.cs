using MediatR;

namespace AccessControl.Application.AccessProfiles.Commands;

public record DeleteAccessProfileCommand(Guid Id) : IRequest;
