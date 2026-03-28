namespace AccessControl.Application.Devices.Abstractions;

public interface ICardReaderCapability
{
    Task<string?> ReadCardAsync(CancellationToken cancellationToken);
}
