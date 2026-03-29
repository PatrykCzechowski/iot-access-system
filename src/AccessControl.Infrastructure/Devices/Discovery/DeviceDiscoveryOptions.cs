namespace AccessControl.Infrastructure.Devices.Discovery;

public sealed class DeviceDiscoveryOptions
{
    public const string SectionName = "DeviceDiscovery";

    public int MqttCacheTtlMinutes { get; set; } = 5;
}
