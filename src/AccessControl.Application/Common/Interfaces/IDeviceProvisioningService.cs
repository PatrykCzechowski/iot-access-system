using AccessControl.Application.Devices.DTOs;

namespace AccessControl.Application.Common.Interfaces;

public interface IDeviceProvisioningService
{
    Task<DeviceProvisionResult> ProvisionAsync(string deviceIp, CancellationToken cancellationToken = default);
}
