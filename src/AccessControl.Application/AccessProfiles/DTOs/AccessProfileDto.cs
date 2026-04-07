namespace AccessControl.Application.AccessProfiles.DTOs;

public record AccessProfileDto(
    Guid Id,
    string Name,
    string? Description,
    int CardholderCount,
    IReadOnlyCollection<Guid> ZoneIds);
