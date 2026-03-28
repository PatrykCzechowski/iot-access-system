using AccessControl.Application.Devices.DTOs;
using MediatR;

namespace AccessControl.Application.Devices.Commands;

public record ScanForDevicesCommand : IRequest<IReadOnlyCollection<DiscoveredDeviceDto>>;
