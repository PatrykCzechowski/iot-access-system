using AccessControl.Domain.Exceptions;

namespace AccessControl.Domain.Entities;

public class AccessProfile
{
    private AccessProfile() { }

    public Guid Id { get; init; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; private set; }

    public ICollection<Cardholder> Cardholders { get; } = new List<Cardholder>();
    public ICollection<AccessProfileZone> AccessProfileZones { get; } = new List<AccessProfileZone>();

    public static AccessProfile Create(string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > 100)
        {
            throw new DomainValidationException("Profile name cannot exceed 100 characters.");
        }

        var now = DateTime.UtcNow;

        return new AccessProfile
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > 100)
        {
            throw new DomainValidationException("Profile name cannot exceed 100 characters.");
        }

        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
