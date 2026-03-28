namespace AccessControl.Application.Devices.Abstractions;

public interface ILockControlCapability
{
    Task OpenAsync(CancellationToken cancellationToken);
    Task CloseAsync(CancellationToken cancellationToken);
}
