namespace Linksoft.PowerController.HostAgent.Services;

/// <summary>
/// Embedded MQTT broker using MQTTnet server.
/// </summary>
public sealed class EmbeddedMqttBroker : IHostedService, IDisposable
{
    private readonly ILogger<EmbeddedMqttBroker> logger;
    private readonly MqttOptions options;
    private MqttServer? server;

    public EmbeddedMqttBroker(
        ILogger<EmbeddedMqttBroker> logger,
        IOptions<MqttOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.logger = logger;
        this.options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var port = options.Embedded.Port;

        logger.LogInformation("Starting embedded MQTT broker on port {Port}", port);

        var optionsBuilder = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(port);

        server = new MqttServerFactory().CreateMqttServer(optionsBuilder.Build());

        server.ClientConnectedAsync += args =>
        {
            logger.LogInformation("MQTT client connected: {ClientId}", args.ClientId);
            return Task.CompletedTask;
        };

        server.ClientDisconnectedAsync += args =>
        {
            logger.LogInformation("MQTT client disconnected: {ClientId}, Reason: {Reason}", args.ClientId, args.DisconnectType);
            return Task.CompletedTask;
        };

        await server
            .StartAsync()
            .ConfigureAwait(false);

        logger.LogInformation("Embedded MQTT broker started successfully on port {Port}", port);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (server is not null)
        {
            logger.LogInformation("Stopping embedded MQTT broker");

            await server
                .StopAsync()
                .ConfigureAwait(false);

            logger.LogInformation("Embedded MQTT broker stopped");
        }
    }

    public void Dispose()
    {
        server?.Dispose();
    }
}