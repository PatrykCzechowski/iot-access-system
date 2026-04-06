using AccessControl.UI.Models;
using Flurl.Http;

namespace AccessControl.UI.Services;

public sealed class DeviceService(IFlurlClient flurlClient) : IDeviceService
{
    public Task<List<DeviceItem>> GetDevicesAsync()
        => flurlClient.Request("api/devices").GetJsonAsync<List<DeviceItem>>();

    public Task AddDeviceAsync(Guid hardwareId, string name, Guid zoneId)
        => flurlClient.Request("api/devices")
            .PostJsonAsync(new { HardwareId = hardwareId, Name = name, ZoneId = zoneId });

    public Task StartEnrollmentAsync(Guid deviceId)
        => flurlClient.Request("api/devices", deviceId, "enrollment", "start").PostAsync();

    public Task CancelEnrollmentAsync(Guid deviceId)
        => flurlClient.Request("api/devices", deviceId, "enrollment", "cancel").PostAsync();

    public Task<DeviceProvisionResult> ProvisionDeviceAsync(Guid deviceId)
        => flurlClient.Request("api/devices", deviceId, "provision")
            .PostAsync()
            .ReceiveJson<DeviceProvisionResult>();
}
