using AccessControl.Domain.Enums;

namespace AccessControl.Application.Devices.Abstractions;

public interface IDeviceAdapterResolver
{
    IDeviceAdapter Resolve(DeviceAdapterType adapterType);
    IReadOnlyCollection<IDeviceAdapter> ResolveByFeatures(DeviceFeatures requiredFeatures);
}
