using AccessControl.Application.Devices.Abstractions;
using AccessControl.Domain.Enums;

namespace AccessControl.Infrastructure.Devices;

public sealed class DeviceAdapterResolver(IEnumerable<IDeviceAdapter> adapters) : IDeviceAdapterResolver
{
    private readonly IReadOnlyCollection<IDeviceAdapter> _adapters = adapters.ToArray();

    public IDeviceAdapter Resolve(DeviceAdapterType adapterType)
    {
        var adapter = _adapters.FirstOrDefault(x => x.AdapterType == adapterType);

        return adapter ?? throw new InvalidOperationException($"Adapter '{adapterType}' was not found.");
    }

    public IReadOnlyCollection<IDeviceAdapter> ResolveByFeatures(DeviceFeatures requiredFeatures)
    {
        if (requiredFeatures == DeviceFeatures.None)
        {
            throw new ArgumentException("Cannot resolve adapters for DeviceFeatures.None.", nameof(requiredFeatures));
        }

        return _adapters
            .Where(x => (x.SupportedFeatures & requiredFeatures) == requiredFeatures)
            .ToArray();
    }
}
