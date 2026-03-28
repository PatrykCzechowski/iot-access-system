namespace AccessControl.Application.Devices.Abstractions;

public interface IKeypadCapability
{
    Task<string?> ReadInputAsync(CancellationToken cancellationToken);
}
