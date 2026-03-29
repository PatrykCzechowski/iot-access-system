using AccessControl.Domain.Exceptions;

namespace AccessControl.Domain.Entities;

public class AccessCard
{
    private AccessCard() { }

    public Guid Id { get; init; }
    public required string CardUid { get; init; }
    public string? UserId { get; private set; }
    public Guid ZoneId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Label { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; private set; }

    public static string NormalizeUid(string cardUid) => cardUid.Trim().ToUpperInvariant();

    public static AccessCard Create(
        string cardUid,
        Guid zoneId,
        string? label = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardUid);
        if (zoneId == Guid.Empty)
        {
            throw new DomainValidationException("ZoneId cannot be empty.");
        }

        var now = DateTime.UtcNow;

        return new AccessCard
        {
            Id = Guid.NewGuid(),
            CardUid = NormalizeUid(cardUid),
            ZoneId = zoneId,
            Label = label?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void AssignUser(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        UserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnassignUser()
    {
        UserId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(Guid zoneId, string? label, bool isActive)
    {
        if (zoneId == Guid.Empty)
        {
            throw new DomainValidationException("ZoneId cannot be empty.");
        }

        ZoneId = zoneId;
        Label = label?.Trim();
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
