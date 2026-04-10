namespace AccessControl.Application.Common.Interfaces;

public record CardholderSummary(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    int CardCount,
    IReadOnlyCollection<Guid> AccessProfileIds);
