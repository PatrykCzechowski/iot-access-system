namespace AccessControl.UI.Models;

public sealed record ZoneItem(
    Guid Id,
    string Name,
    string? Description,
    int DeviceCount,
    int CardCount,
    DateTime CreatedAt);
