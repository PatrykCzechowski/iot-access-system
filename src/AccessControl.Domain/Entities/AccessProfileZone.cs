namespace AccessControl.Domain.Entities;

public class AccessProfileZone
{
    public Guid AccessProfileId { get; init; }
    public AccessProfile AccessProfile { get; private set; } = null!;

    public Guid AccessZoneId { get; init; }
    public AccessZone AccessZone { get; private set; } = null!;
}
