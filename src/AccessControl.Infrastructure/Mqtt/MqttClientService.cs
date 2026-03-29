using System.Text;
using AccessControl.Application.Common;
using AccessControl.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace AccessControl.Infrastructure.Mqtt;

public sealed class MqttClientService : BackgroundService, IMqttService
{
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _clientOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MqttClientService> _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    public MqttClientService(
        IOptions<MqttOptions> options,
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        ILogger<MqttClientService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        var opts = options.Value;
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(opts.Host, opts.Port)
            .WithClientId(opts.ClientId)
            .WithCleanSession(true)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30));

        if (!string.IsNullOrWhiteSpace(opts.Username))
        {
            builder.WithCredentials(opts.Username, opts.Password);
        }

        if (opts.UseTls)
        {
            if (opts.AllowUntrustedCertificates && !environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "MQTT AllowUntrustedCertificates cannot be enabled in non-Development environments.");
            }

            if (opts.AllowUntrustedCertificates)
            {
                logger.LogWarning("MQTT TLS certificate validation is DISABLED (Development only)");
            }

            builder.WithTlsOptions(tls =>
            {
                if (opts.AllowUntrustedCertificates)
                {
                    tls.WithCertificateValidationHandler(_ => true);
                }
            });
        }

        _clientOptions = builder.Build();

        _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _client.DisconnectedAsync += OnDisconnectedAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() => _cts.Cancel());
        await ConnectWithRetryAsync(_cts.Token);
    }

    public async Task PublishAsync(string topic, string payload, bool retain = false, CancellationToken cancellationToken = default)
    {
        if (!_client.IsConnected)
        {
            _logger.LogError("MQTT client not connected. Failed to publish to {Topic}", topic);
            throw new InvalidOperationException($"MQTT client is not connected. Cannot publish to '{topic}'.");
        }

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(retain)
            .Build();

        await _client.PublishAsync(message, cancellationToken);
        _logger.LogDebug("Published to {Topic} ({Length} bytes)", topic, payload.Length);
    }

    private async Task ConnectWithRetryAsync(CancellationToken ct)
    {
        if (!await _connectLock.WaitAsync(0, ct))
        {
            return;
        }

        try
        {
            var delay = TimeSpan.FromSeconds(2);
            var maxDelay = TimeSpan.FromSeconds(30);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _client.ConnectAsync(_clientOptions, ct);
                    _logger.LogInformation("MQTT connected to broker");

                    await SubscribeAsync(ct);
                    return;
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "MQTT connection failed. Retrying in {Delay}s...", delay.TotalSeconds);
                    await Task.Delay(delay, ct);
                    delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelay.TotalSeconds));
                }
            }
        }
        finally
        {
            _connectLock.Release();
        }
    }

    private async Task SubscribeAsync(CancellationToken ct)
    {
        var builder = new MqttClientSubscribeOptionsBuilder();
        foreach (var topic in MqttTopics.SubscribePatterns)
        {
            builder.WithTopicFilter(topic, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        await _client.SubscribeAsync(builder.Build(), ct);
        _logger.LogInformation("Subscribed to {Count} MQTT topics", MqttTopics.SubscribePatterns.Length);
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = e.ApplicationMessage.PayloadSegment.Count > 0
            ? Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)
            : string.Empty;

        _logger.LogDebug("MQTT received [{Topic}]: {Payload}", topic, payload);

        using var scope = _scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IMqttMessageHandler>();

        foreach (var handler in handlers)
        {
            if (handler.CanHandle(topic))
            {
                try
                {
                    await handler.HandleAsync(topic, payload, _cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in {Handler} for topic {Topic}", handler.GetType().Name, topic);
                }
            }
        }
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        if (e.ClientWasConnected && !_cts.IsCancellationRequested)
        {
            _logger.LogWarning("MQTT disconnected. Reconnecting...");
            await ConnectWithRetryAsync(_cts.Token);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cts.CancelAsync();
        await base.StopAsync(cancellationToken);

        if (_client.IsConnected)
        {
            await _client.DisconnectAsync(cancellationToken: cancellationToken);
        }

        _client.Dispose();
        _cts.Dispose();
        _connectLock.Dispose();
    }
}
