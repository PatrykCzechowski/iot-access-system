using AccessControl.Application.Devices.Abstractions;
using AccessControl.Domain.Enums;

namespace AccessControl.Infrastructure.Devices.Adapters;

public sealed class CardReaderDeviceAdapter : IDeviceAdapter, ICardReaderCapability
{
    public DeviceAdapterType AdapterType => DeviceAdapterType.CardReader;
    public DeviceFeatures SupportedFeatures => DeviceFeatures.CardReader;

    public Task<string?> ReadCardAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }
}
