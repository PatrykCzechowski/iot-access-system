using AccessControl.Application.Devices.DTOs;
using MediatR;

namespace AccessControl.Application.Devices.Queries;

public record GetDeviceByIdQuery(Guid Id) : IRequest<DeviceDto>;
