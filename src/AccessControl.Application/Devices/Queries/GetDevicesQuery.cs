using AccessControl.Application.Devices.DTOs;
using MediatR;

namespace AccessControl.Application.Devices.Queries;

public record GetDevicesQuery : IRequest<IReadOnlyCollection<DeviceDto>>;
