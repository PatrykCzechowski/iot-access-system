namespace AccessControl.Infrastructure.Mqtt;

public sealed class MqttOptions
{
    public const string SectionName = "Mqtt";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string ClientId { get; set; } = "access-control-server";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseTls { get; set; }
    public bool AllowUntrustedCertificates { get; set; }
}
