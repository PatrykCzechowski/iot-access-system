using AccessControl.UI.Models;

namespace AccessControl.UI.Services;

public interface IDeviceService
{
    Task<List<DeviceItem>> GetDevicesAsync();
    Task AddDeviceAsync(Guid hardwareId, string name, Guid zoneId);
    Task StartEnrollmentAsync(Guid deviceId);
    Task CancelEnrollmentAsync(Guid deviceId);
}
