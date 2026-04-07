namespace AccessControl.Domain.Entities;

public class Cardholder
{
    private Cardholder() { }

    public Guid Id { get; init; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; private set; }

    public ICollection<AccessCard> Cards { get; } = new List<AccessCard>();
    public ICollection<AccessProfile> AccessProfiles { get; } = new List<AccessProfile>();

    public static Cardholder Create(string firstName, string lastName, string? email = null, string? phoneNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        var now = DateTime.UtcNow;

        return new Cardholder
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email?.Trim(),
            PhoneNumber = phoneNumber?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(string firstName, string lastName, string? email, string? phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email?.Trim();
        PhoneNumber = phoneNumber?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public string FullName => $"{FirstName} {LastName}";
}
