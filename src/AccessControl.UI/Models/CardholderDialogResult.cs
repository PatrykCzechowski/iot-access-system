namespace AccessControl.UI.Models;

public sealed record CardholderDialogResult(
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber);
