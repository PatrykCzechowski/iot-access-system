using AccessControl.Application.Devices.Abstractions;
using AccessControl.Domain.Enums;

namespace AccessControl.Infrastructure.Devices.Adapters;

public sealed class DisplayExecutorDeviceAdapter : IDeviceAdapter, IDisplayCapability
{
    public DeviceAdapterType AdapterType => DeviceAdapterType.DisplayExecutor;
    public DeviceFeatures SupportedFeatures => DeviceFeatures.Display;

    public Task ShowMessageAsync(string message, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return Task.CompletedTask;
    }
}
