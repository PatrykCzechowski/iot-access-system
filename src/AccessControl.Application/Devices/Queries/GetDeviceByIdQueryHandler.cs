using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.Devices.DTOs;
using MediatR;

namespace AccessControl.Application.Devices.Queries;

public sealed class GetDeviceByIdQueryHandler(IDeviceRepository repository)
    : IRequestHandler<GetDeviceByIdQuery, DeviceDto>
{
    public async Task<DeviceDto> Handle(
        GetDeviceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var device = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Device '{request.Id}' was not found.");

        return new DeviceDto(
            device.Id,
            device.HardwareId,
            device.Name,
            device.AdapterType,
            device.Features,
            device.ZoneId,
            device.Status.ToString(),
            device.CreatedAt,
            device.UpdatedAt);
    }
}
