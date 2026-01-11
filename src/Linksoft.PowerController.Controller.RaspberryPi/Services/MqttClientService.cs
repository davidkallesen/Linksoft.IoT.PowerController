namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// MQTT client service for communicating with HostAgents.
/// </summary>
[Registration(Lifetime.Singleton, As = typeof(IMqttClientService), Condition = "Mqtt:Enabled")]
public sealed class MqttClientService : IMqttClientService, IDisposable
{
    private const int ConnectionLockTimeoutInMs = 30_000;
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<HostAgentSystemInfo?>> pendingInfoRequests = new(StringComparer.CurrentCulture);
    private readonly MqttOptions options;
    private readonly ILogger<MqttClientService> logger;
    private IMqttClient? client;
    private Task? reconnectTask;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public MqttClientService(
        IOptions<MqttOptions> options,
        ILogger<MqttClientService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options.Value;
        this.logger = logger;
    }

    public bool IsConnected => client?.IsConnected ?? false;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting MQTT client service");

        client = new MqttClientFactory().CreateMqttClient();

        client.ConnectedAsync += OnConnectedAsync;
        client.DisconnectedAsync += OnDisconnectedAsync;
        client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

        await ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping MQTT client service");

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
    }

    public async Task SendShutdownRequestAsync(
        string hostname,
        CancellationToken cancellationToken = default)
    {
        if (client is null || !client.IsConnected)
        {
            logger.LogWarning("MQTT client not connected, cannot send shutdown request");
            return;
        }

        var topic = $"{options.BaseTopic}/{hostname.ToLowerInvariant()}/shutdown/request";
        var payload = JsonSerializer.Serialize(new { mode = "Immediate" }, JsonOptions);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await client
            .PublishAsync(message, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Sent shutdown request to {Hostname} via MQTT", hostname);
    }

    public async Task<HostAgentSystemInfo?> RequestInfoAsync(
        string hostname,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (client is null || !client.IsConnected)
        {
            logger.LogWarning("MQTT client not connected, cannot request info");
            return null;
        }

        var tcs = new TaskCompletionSource<HostAgentSystemInfo?>();
        var key = hostname.ToLowerInvariant();

        if (!pendingInfoRequests.TryAdd(key, tcs))
        {
            logger.LogWarning("Info request already pending for {Hostname}", hostname);
            return null;
        }

        try
        {
            var topic = $"{options.BaseTopic}/{key}/info/request";

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(string.Empty)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await client
                .PublishAsync(message, cancellationToken)
                .ConfigureAwait(false);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                return await tcs
                    .Task
                    .WaitAsync(cts.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug("Info request timed out for {Hostname}", hostname);
                return null;
            }
        }
        finally
        {
            pendingInfoRequests.TryRemove(key, out _);
        }
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var lockAcquired = false;

        try
        {
            if (client is null || client.IsConnected)
            {
                return;
            }

            lockAcquired = await connectionLock
                .WaitAsync(ConnectionLockTimeoutInMs, cancellationToken)
                .ConfigureAwait(false);

            var clientOptions = new MqttClientOptionsBuilder()
                .WithClientId($"raspberrypi-controller-{Environment.MachineName.ToLowerInvariant()}")
                .WithTcpServer(options.Host, options.Port)
                .WithCleanSession()
                .Build();

            if (options.UseTls)
            {
                clientOptions = new MqttClientOptionsBuilder()
                    .WithClientId($"raspberrypi-controller-{Environment.MachineName.ToLowerInvariant()}")
                    .WithTcpServer(options.Host, options.Port)
                    .WithTlsOptions(o => o.WithCertificateValidationHandler(_ => true))
                    .WithCleanSession()
                    .Build();
            }

            if (!string.IsNullOrEmpty(options.Username))
            {
                clientOptions = new MqttClientOptionsBuilder()
                    .WithClientId($"raspberrypi-controller-{Environment.MachineName.ToLowerInvariant()}")
                    .WithTcpServer(options.Host, options.Port)
                    .WithCredentials(options.Username, options.Password)
                    .WithCleanSession()
                    .Build();
            }

            logger.LogInformation(
                "Connecting to MQTT broker at {Host}:{Port}",
                options.Host,
                options.Port);

            await client
                .ConnectAsync(clientOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to MQTT broker");
            ScheduleReconnect();
        }
        finally
        {
            if (lockAcquired)
            {
                connectionLock.Release();
            }
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

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs args)
    {
        logger.LogInformation("MQTT client connected to broker");

        // Subscribe to info responses from all hosts
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter($"{options.BaseTopic}/+/info/response")
            .Build();

        await client!
            .SubscribeAsync(subscribeOptions)
            .ConfigureAwait(false);

        logger.LogInformation("Subscribed to MQTT info response topics");
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        logger.LogWarning("MQTT client disconnected: {Reason}", args.Reason);
        ScheduleReconnect();
        return Task.CompletedTask;
    }

    private Task OnMessageReceivedAsync(
        MqttApplicationMessageReceivedEventArgs args)
    {
        var topic = args.ApplicationMessage.Topic;
        var payload = args.ApplicationMessage.ConvertPayloadToString();

        // Parse topic: {baseTopic}/{hostname}/info/response
        var parts = topic.Split('/');
        if (parts.Length >= 3 && parts[^1] == "response" && parts[^2] == "info")
        {
            var hostname = parts[^3];

            if (pendingInfoRequests.TryGetValue(hostname, out var tcs))
            {
                try
                {
                    var info = JsonSerializer.Deserialize<HostAgentSystemInfo>(payload, JsonOptions);
                    tcs.TrySetResult(info);
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to parse info response from {Hostname}", hostname);
                    tcs.TrySetResult(null);
                }
            }
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        client?.Dispose();
        connectionLock.Dispose();
    }
}