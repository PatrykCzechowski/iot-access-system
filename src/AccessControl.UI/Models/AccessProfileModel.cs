namespace AccessControl.UI.Models;

public sealed record AccessProfileItem(
    Guid Id,
    string Name,
    string? Description,
    int CardholderCount,
    List<Guid> ZoneIds);
