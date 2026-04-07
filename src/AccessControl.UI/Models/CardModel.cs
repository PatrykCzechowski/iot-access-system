namespace AccessControl.UI.Models;

public sealed record CardItem(
    Guid Id,
    string CardUid,
    Guid? CardholderId,
    string? CardholderName,
    bool IsActive,
    string? Label,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public static class CardHelpers
{
    public static string FormatIsActive(bool isActive)
    {
        return isActive ? "Active" : "Inactive";
    }
}
