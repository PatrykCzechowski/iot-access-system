using AccessControl.UI.Models;

namespace AccessControl.UI.Services;

public interface IAccessHubService : IAsyncDisposable
{
    event Action<AccessLogItem>? OnCardScanned;
    event Action<CardEnrolledItem>? OnCardEnrolled;
    event Action<bool>? OnConnectionChanged;
    bool IsConnected { get; }
    Task StartAsync();
    Task StopAsync();
}
