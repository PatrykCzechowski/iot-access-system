using System.Text.RegularExpressions;

namespace AccessControl.Application.Common;

public static partial class MqttTopics
{
    private const string Prefix = "accesscontrol";

    public static bool TryExtractHardwareId(string topic, out Guid hardwareId)
    {
        var match = HardwareIdPattern().Match(topic);
        if (match.Success && Guid.TryParse(match.Groups[1].Value, out hardwareId))
        {
            return true;
        }

        hardwareId = Guid.Empty;
        return false;
    }

    [GeneratedRegex(@"^accesscontrol/([^/]+)/")]
    private static partial Regex HardwareIdPattern();

    // Publish topics (server → device)
    public static string LockCommand(Guid hardwareId) => $"{Prefix}/{hardwareId}/lock/command";
    public static string CardEnroll(Guid hardwareId) => $"{Prefix}/{hardwareId}/card/enroll";
    public static string ConfigSet(Guid hardwareId) => $"{Prefix}/{hardwareId}/config/set";

    // Subscribe patterns (device → server, MQTT wildcards)
    public static readonly string[] SubscribePatterns =
    [
        $"{Prefix}/+/announce",
        $"{Prefix}/+/heartbeat",
        $"{Prefix}/+/card/scanned",
        $"{Prefix}/+/card/enrolled",
        $"{Prefix}/+/config/ack",
        $"{Prefix}/+/lock/status"
    ];
}
