namespace AccessControl.Application.Common.Interfaces;

public interface IMqttService
{
    Task PublishAsync(string topic, string payload, bool retain = false, CancellationToken cancellationToken = default);
}
