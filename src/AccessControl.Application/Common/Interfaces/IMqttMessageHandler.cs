namespace AccessControl.Application.Common.Interfaces;

public interface IMqttMessageHandler
{
    bool CanHandle(string topic);
    Task HandleAsync(string topic, string payload, CancellationToken cancellationToken);
}
