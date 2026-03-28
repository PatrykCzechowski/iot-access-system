using AccessControl.Application.Devices.Abstractions;
using AccessControl.Domain.Enums;

namespace AccessControl.Infrastructure.Devices.Adapters;

public sealed class CardAndKeypadReaderDeviceAdapter : IDeviceAdapter, ICardReaderCapability, IKeypadCapability
{
    public DeviceAdapterType AdapterType => DeviceAdapterType.CardAndKeypadReader;
    public DeviceFeatures SupportedFeatures => DeviceFeatures.CardReader | DeviceFeatures.Keypad;

    public Task<string?> ReadCardAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }

    public Task<string?> ReadInputAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }
}
