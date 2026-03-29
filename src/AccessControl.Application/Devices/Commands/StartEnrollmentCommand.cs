using MediatR;

namespace AccessControl.Application.Devices.Commands;

public record StartEnrollmentCommand(Guid DeviceId) : IRequest;
