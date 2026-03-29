namespace AccessControl.Application.Cards.DTOs;

public record AccessCardDto(
    Guid Id,
    string CardUid,
    string? UserId,
    Guid ZoneId,
    bool IsActive,
    string? Label,
    DateTime CreatedAt,
    DateTime UpdatedAt);
