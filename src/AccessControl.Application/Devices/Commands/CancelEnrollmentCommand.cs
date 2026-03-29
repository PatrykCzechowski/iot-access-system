using MediatR;

namespace AccessControl.Application.Devices.Commands;

public record CancelEnrollmentCommand(Guid DeviceId) : IRequest;
