using AccessControl.Application.Devices.Abstractions;
using AccessControl.Domain.Enums;

namespace AccessControl.Infrastructure.Devices.Adapters;

public sealed class CardReaderWithLockDeviceAdapter : IDeviceAdapter, ICardReaderCapability, ILockControlCapability
{
    public DeviceAdapterType AdapterType => DeviceAdapterType.CardReaderWithLock;
    public DeviceFeatures SupportedFeatures => DeviceFeatures.CardReader | DeviceFeatures.LockControl;

    public Task<string?> ReadCardAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(null);
    }

    public Task OpenAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task CloseAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
