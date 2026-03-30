namespace AccessControl.UI.Models;

public sealed record DeviceItem(
    Guid Id,
    Guid HardwareId,
    string Name,
    int AdapterType,
    int Features,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public static class DeviceHelpers
{
    public static bool IsOnline(string status) =>
        string.Equals(status, "Online", StringComparison.OrdinalIgnoreCase);

    public static string FormatAdapterType(int adapterType) => adapterType switch
    {
        1 => "Card Reader",
        2 => "Keypad Reader",
        3 => "Card & Keypad Reader",
        4 => "Display Executor",
        5 => "Lock Pin Executor",
        _ => $"Unknown ({adapterType})"
    };

    public static IEnumerable<string> GetFeatures(int features) =>
        new[]
        {
            (1,   "Card Reader"),
            (2,   "Keypad"),
            (4,   "Biometrics"),
            (8,   "Camera"),
            (16,  "Lock Control"),
            (32,  "Display"),
            (64,  "Intercom"),
            (128, "Motion Sensor"),
        }
        .Where(f => (features & f.Item1) != 0)
        .Select(f => f.Item2);
}
