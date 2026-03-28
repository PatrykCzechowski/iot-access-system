using AccessControl.Application.Devices.Abstractions;
using AccessControl.Domain.Enums;

namespace AccessControl.Infrastructure.Devices.Adapters;

public sealed class LockPinExecutorDeviceAdapter : IDeviceAdapter, ILockControlCapability
{
    public DeviceAdapterType AdapterType => DeviceAdapterType.LockPinExecutor;
    public DeviceFeatures SupportedFeatures => DeviceFeatures.LockControl;

    public Task OpenAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task CloseAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
