namespace Linksoft.PowerController.HostAgent.Services;

/// <summary>
/// MQTT service for publishing status and handling commands.
/// </summary>
public sealed class MqttService : IMqttService, IDisposable
{
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private readonly MqttOptions options;
    private readonly ISystemService systemService;
    private readonly ILogger<MqttService> logger;
    private readonly string hostname;
    private IMqttClient? client;
    private PeriodicTimer? statusTimer;
    private Task? statusTimerTask;
    private CancellationTokenSource? statusTimerCts;
    private Task? reconnectTask;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public MqttService(
        ILogger<MqttService> logger,
        IOptions<MqttOptions> options,
        ISystemService systemService)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.logger = logger;
        this.options = options.Value;
        this.systemService = systemService;
        hostname = Environment.MachineName.ToLowerInvariant();
    }

    public bool IsConnected => client?.IsConnected ?? false;

    private string TopicPrefix => $"{options.Topics.BaseTopic}/{hostname}";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting MQTT service for hostname {Hostname}", hostname);

        client = new MqttClientFactory().CreateMqttClient();

        client.ConnectedAsync += OnConnectedAsync;
        client.DisconnectedAsync += OnDisconnectedAsync;
        client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

        await ConnectAsync(cancellationToken).ConfigureAwait(false);

        // Start periodic status publishing
        var interval = TimeSpan.FromSeconds(options.Topics.StatusInterval);
        statusTimerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        statusTimer = new PeriodicTimer(interval);
        statusTimerTask = RunStatusTimerAsync(statusTimerCts.Token);

        logger.LogInformation("MQTT status publishing enabled with interval {Interval} seconds", options.Topics.StatusInterval);
    }

    private async Task RunStatusTimerAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await statusTimer!
                .WaitForNextTickAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                await PublishStatusAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping MQTT service");

        // Stop status timer
        if (statusTimerCts is not null)
        {
            await statusTimerCts
                .CancelAsync()
                .ConfigureAwait(false);
        }

        if (statusTimerTask is not null)
        {
            await statusTimerTask.ConfigureAwait(false);
            statusTimerTask = null;
        }

        statusTimer?.Dispose();
        statusTimer = null;
        statusTimerCts?.Dispose();
        statusTimerCts = null;

        // Wait for any pending reconnect
        if (reconnectTask is not null)
        {
            await reconnectTask.ConfigureAwait(false);
            reconnectTask = null;
        }

        if (client is not null && client.IsConnected)
        {
            await client
                .DisconnectAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        logger.LogInformation("MQTT service stopped");
    }

    public async Task PublishStatusAsync(
        CancellationToken cancellationToken = default)
    {
        if (client is null || !client.IsConnected)
        {
            return;
        }

        try
        {
            var systemInfo = systemService.GetSystemInfo();
            var status = new
            {
                systemInfo.Hostname,
                systemInfo.ServiceUptime,
                systemInfo.ServerUptime,
                systemInfo.OperatingSystem,
                systemInfo.ShutdownInProgress,
                systemInfo.ShutdownScheduledAt,
                Timestamp = DateTimeOffset.UtcNow,
            };

            var payload = JsonSerializer.Serialize(status, JsonOptions);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"{TopicPrefix}/status")
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag()
                .Build();

            await client
                .PublishAsync(message, cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug("Published status to {Topic}", $"{TopicPrefix}/status");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing MQTT status");
        }
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        await connectionLock
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            if (client is null || client.IsConnected)
            {
                return;
            }

            var clientOptions = BuildClientOptions();

            logger.LogInformation("Connecting to MQTT broker...");

            await client
                .ConnectAsync(clientOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to MQTT broker, will retry");
            ScheduleReconnect();
        }
        finally
        {
            connectionLock.Release();
        }
    }

    private void ScheduleReconnect()
    {
        if (reconnectTask is not null && !reconnectTask.IsCompleted)
        {
            return;
        }

        reconnectTask = ReconnectAfterDelayAsync();
    }

    private async Task ReconnectAfterDelayAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            await ConnectAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Reconnection attempt failed");
        }
    }

    private MqttClientOptions BuildClientOptions()
    {
        var builder = new MqttClientOptionsBuilder()
            .WithClientId($"powercontroller-{hostname}")
            .WithCleanSession();

        if (options.IsEmbeddedMode)
        {
            builder.WithTcpServer("localhost", options.Embedded.Port);
            logger.LogInformation(
                "MQTT configured for embedded broker on port {Port}",
                options.Embedded.Port);
        }
        else
        {
            builder.WithTcpServer(options.External.Host, options.External.Port);

            if (options.External.UseTls)
            {
                builder.WithTlsOptions(o => o.WithCertificateValidationHandler(_ => true));
            }

            if (!string.IsNullOrEmpty(options.External.Username))
            {
                builder.WithCredentials(options.External.Username, options.External.Password);
            }

            logger.LogInformation(
                "MQTT configured for external broker at {Host}:{Port}",
                options.External.Host,
                options.External.Port);
        }

        return builder.Build();
    }

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs args)
    {
        logger.LogInformation("MQTT client connected to broker");

        // Subscribe to command topics
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter($"{TopicPrefix}/info/request")
            .WithTopicFilter($"{TopicPrefix}/shutdown/request")
            .Build();

        await client!
            .SubscribeAsync(subscribeOptions)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Subscribed to MQTT topics: {InfoTopic}, {ShutdownTopic}",
            $"{TopicPrefix}/info/request",
            $"{TopicPrefix}/shutdown/request");

        // Publish initial status
        await PublishStatusAsync().ConfigureAwait(false);
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        logger.LogWarning("MQTT client disconnected: {Reason}", args.Reason);
        ScheduleReconnect();
        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(
        MqttApplicationMessageReceivedEventArgs args)
    {
        var topic = args.ApplicationMessage.Topic;
        var payload = args.ApplicationMessage.ConvertPayloadToString();

        logger.LogInformation("MQTT message received on topic {Topic}", topic);

        try
        {
            if (topic.EndsWith("/info/request", StringComparison.OrdinalIgnoreCase))
            {
                await HandleInfoRequestAsync().ConfigureAwait(false);
            }
            else if (topic.EndsWith("/shutdown/request", StringComparison.OrdinalIgnoreCase))
            {
                await HandleShutdownRequestAsync(payload).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling MQTT message on topic {Topic}", topic);
        }
    }

    private async Task HandleInfoRequestAsync()
    {
        var systemInfo = systemService.GetSystemInfo();
        var payload = JsonSerializer.Serialize(systemInfo, JsonOptions);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"{TopicPrefix}/info/response")
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await client!
            .PublishAsync(message)
            .ConfigureAwait(false);

        logger.LogInformation("Published info response to {Topic}", $"{TopicPrefix}/info/response");
    }

    private async Task HandleShutdownRequestAsync(string payload)
    {
        ShutdownRequest? request = null;

        if (!string.IsNullOrWhiteSpace(payload))
        {
            request = JsonSerializer.Deserialize<ShutdownRequest>(payload, JsonOptions);
        }

        var mode = request?.Mode ?? ShutdownMode.Immediate;

        object response;

        if (systemService.IsShutdownInProgress)
        {
            response = new
            {
                Success = false,
                Message = "Shutdown already in progress",
                systemService.ShutdownScheduledAt,
            };

            logger.LogWarning("MQTT shutdown request rejected: Shutdown already in progress");
        }
        else
        {
            var shutdownResponse = await systemService
                .InitiateShutdownAsync(mode, request?.DelaySeconds, request?.ScheduledAt)
                .ConfigureAwait(false);

            response = new
            {
                Success = true,
                shutdownResponse.Message,
                shutdownResponse.ShutdownScheduledAt,
            };

            logger.LogInformation("MQTT shutdown initiated: ScheduledAt={ScheduledAt}", shutdownResponse.ShutdownScheduledAt);
        }

        var responsePayload = JsonSerializer.Serialize(response, JsonOptions);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"{TopicPrefix}/shutdown/response")
            .WithPayload(responsePayload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await client!
            .PublishAsync(message)
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        statusTimerCts?.Cancel();
        statusTimerCts?.Dispose();
        statusTimer?.Dispose();
        client?.Dispose();
        connectionLock.Dispose();
    }
}