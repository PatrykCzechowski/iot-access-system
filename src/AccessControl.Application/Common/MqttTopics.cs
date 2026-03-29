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

    // Publish topics (concrete device)
    public static string CardResult(Guid hardwareId) => $"{Prefix}/{hardwareId}/card/result";
    public static string LockCommand(Guid hardwareId) => $"{Prefix}/{hardwareId}/lock/command";
    public static string EnrollmentStart(Guid hardwareId) => $"{Prefix}/{hardwareId}/enrollment/start";
    public static string EnrollmentCancel(Guid hardwareId) => $"{Prefix}/{hardwareId}/enrollment/cancel";
    public static string EnrollmentResult(Guid hardwareId) => $"{Prefix}/{hardwareId}/enrollment/result";
    public static string ConfigUpdate(Guid hardwareId) => $"{Prefix}/{hardwareId}/config/update";

    // Subscribe patterns (MQTT wildcards)
    public static readonly string[] SubscribePatterns =
    [
        $"{Prefix}/+/announce",
        $"{Prefix}/+/heartbeat",
        $"{Prefix}/+/card/read",
        $"{Prefix}/+/card/enrolled",
        $"{Prefix}/+/config/ack",
        $"{Prefix}/+/lock/status"
    ];
}
