using AccessControl.Application.Common.Interfaces;
using AccessControl.Application.Devices.DTOs;
using MediatR;

namespace AccessControl.Application.Devices.Queries;

public sealed class GetDevicesQueryHandler(IDeviceRepository repository)
    : IRequestHandler<GetDevicesQuery, IReadOnlyCollection<DeviceDto>>
{
    public async Task<IReadOnlyCollection<DeviceDto>> Handle(
        GetDevicesQuery request,
        CancellationToken cancellationToken)
    {
        var devices = await repository.GetAllAsync(cancellationToken);

        return devices
            .Select(d => new DeviceDto(
                d.Id,
                d.HardwareId,
                d.Name,
                d.AdapterType,
                d.Features,
                d.Status.ToString(),
                d.CreatedAt,
                d.UpdatedAt))
            .ToArray();
    }
}
