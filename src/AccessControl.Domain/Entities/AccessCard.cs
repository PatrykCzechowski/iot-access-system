namespace AccessControl.Domain.Entities;

public class AccessCard
{
    private AccessCard() { }

    public Guid Id { get; init; }
    public required string CardUid { get; init; }
    public bool IsActive { get; private set; } = true;
    public string? Label { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; private set; }
    public Guid? CardholderId { get; private set; }
    public Cardholder? Cardholder { get; private set; }

    public static string NormalizeUid(string cardUid) => cardUid.Trim().ToUpperInvariant();

    public static AccessCard Create(
        string cardUid,
        string? label = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cardUid);

        var now = DateTime.UtcNow;

        return new AccessCard
        {
            Id = Guid.NewGuid(),
            CardUid = NormalizeUid(cardUid),
            Label = label?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void AssignCardholder(Guid cardholderId)
    {
        if (cardholderId == Guid.Empty)
        {
            throw new ArgumentException("CardholderId cannot be empty.", nameof(cardholderId));
        }

        CardholderId = cardholderId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnassignCardholder()
    {
        CardholderId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string? label, bool isActive)
    {
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
