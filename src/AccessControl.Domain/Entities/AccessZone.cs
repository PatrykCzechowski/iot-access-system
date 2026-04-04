using AccessControl.Domain.Exceptions;

namespace AccessControl.Domain.Entities;

public class AccessZone
{
    private AccessZone() { }

    public Guid Id { get; init; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; private set; }

    public static AccessZone Create(string name, string? description = null, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > 100)
        {
            throw new DomainValidationException("Zone name cannot exceed 100 characters.");
        }

        if (description is { Length: > 500 })
        {
            throw new DomainValidationException("Zone description cannot exceed 500 characters.");
        }

        var now = DateTime.UtcNow;

        return new AccessZone
        {
            Id = id ?? Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void UpdateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > 100)
        {
            throw new DomainValidationException("Zone name cannot exceed 100 characters.");
        }

        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string? description)
    {
        if (description is { Length: > 500 })
        {
            throw new DomainValidationException("Zone description cannot exceed 500 characters.");
        }

        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
