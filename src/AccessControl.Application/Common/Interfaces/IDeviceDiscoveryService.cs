using AccessControl.Domain.ValueObjects;

namespace AccessControl.Application.Common.Interfaces;

public interface IDeviceDiscoveryService
{
    Task<IReadOnlyCollection<DiscoveredDeviceInfo>> ScanAsync(CancellationToken cancellationToken);
    IReadOnlyCollection<DiscoveredDeviceInfo> GetCached();
    DiscoveredDeviceInfo? GetByHardwareId(Guid hardwareId);
}
