using Makaretu.Dns;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AccessControl.Infrastructure.Mqtt;

/// <summary>
/// Advertises the MQTT broker as _mqtt._tcp via mDNS so ESP32 devices
/// can auto-discover it without hardcoding an IP address.
/// </summary>
public sealed class MqttBrokerMdnsAdvertiser : MdnsAdvertiserBase
{
    private readonly MqttOptions _options;

    public MqttBrokerMdnsAdvertiser(
        IOptions<MqttOptions> options,
        ILogger<MqttBrokerMdnsAdvertiser> logger)
        : base(logger)
    {
        _options = options.Value;
    }

    protected override string ServiceDescription => "MQTT broker";

    protected override ServiceProfile? CreateProfile()
    {
        var brokerIp = NetworkHelper.ResolveAdvertiseAddress(_options.Host);
        if (brokerIp is null)
        {
            return null;
        }

        var port = (ushort)_options.Port;
        var profile = new ServiceProfile("mqtt-broker", "_mqtt._tcp", port);
        profile.AddProperty("server", _options.Host);
        ReplaceAddressRecords(profile, brokerIp);
        return profile;
    }
}

