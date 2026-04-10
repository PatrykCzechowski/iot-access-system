namespace AccessControl.UI.Models;

public sealed record CardholderItem(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    int CardCount,
    List<Guid> AccessProfileIds)
{
    public string FullName => $"{FirstName} {LastName}";
}
