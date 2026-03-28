using AccessControl.Domain.Enums;

namespace AccessControl.Application.Devices.Abstractions;

public interface IDeviceAdapter
{
    DeviceAdapterType AdapterType { get; }
    DeviceFeatures SupportedFeatures { get; }
}
