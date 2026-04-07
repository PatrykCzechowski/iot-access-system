namespace AccessControl.Application.Cards.DTOs;

public record AccessCardDto(
    Guid Id,
    string CardUid,
    Guid? CardholderId,
    string? CardholderName,
    bool IsActive,
    string? Label,
    DateTime CreatedAt,
    DateTime UpdatedAt);
