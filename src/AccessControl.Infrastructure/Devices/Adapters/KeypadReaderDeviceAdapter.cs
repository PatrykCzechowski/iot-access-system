using AccessControl.Application.Devices.Abstractions;
using AccessControl.Domain.Enums;

namespace AccessControl.Infrastructure.Devices.Adapters;

public sealed class KeypadReaderDeviceAdapter : IDeviceAdapter, IKeypadCapability
{
    public DeviceAdapterType AdapterType => DeviceAdapterType.KeypadReader;
    public DeviceFeatures SupportedFeatures => DeviceFeatures.Keypad;

    public Task<string?> ReadInputAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }
}
