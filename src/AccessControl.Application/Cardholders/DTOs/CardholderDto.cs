namespace AccessControl.Application.Cardholders.DTOs;

public record CardholderDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    int CardCount);
