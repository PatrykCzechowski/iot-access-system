using System.Text.Json;
using AccessControl.UI.Auth;
using AccessControl.UI.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace AccessControl.UI.Services;

public sealed class AccessHubService : IAccessHubService
{
    private readonly HubConnection _hubConnection;

    public event Action<AccessLogItem>? OnCardScanned;
    public event Action<CardEnrolledItem>? OnCardEnrolled;
    public event Action<bool>? OnConnectionChanged;
    public bool IsConnected => _hubConnection.State == HubConnectionState.Connected;

    public AccessHubService(IConfiguration configuration, ILocalStorageService localStorage)
    {
        var apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7157/";
        var hubUrl = $"{apiBaseUrl.TrimEnd('/')}/hubs/access-control";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () =>
                    await localStorage.GetItemAsync<string>(AuthConstants.TokenKey);
            })
            .WithAutomaticReconnect()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
            })
            .Build();

        _hubConnection.On<AccessLogItem>("CardScanned", item =>
        {
            OnCardScanned?.Invoke(item);
        });

        _hubConnection.On<CardEnrolledItem>("CardEnrolled", item =>
        {
            OnCardEnrolled?.Invoke(item);
        });

        _hubConnection.Reconnected += _ =>
        {
            OnConnectionChanged?.Invoke(true);
            return Task.CompletedTask;
        };

        _hubConnection.Closed += _ =>
        {
            OnConnectionChanged?.Invoke(false);
            return Task.CompletedTask;
        };
    }

    public Task StartAsync()
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            return _hubConnection.StartAsync();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return _hubConnection.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}
