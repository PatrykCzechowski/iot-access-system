namespace AccessControl.Application.Devices.Abstractions;

public interface IDisplayCapability
{
    Task ShowMessageAsync(string message, CancellationToken cancellationToken);
}
