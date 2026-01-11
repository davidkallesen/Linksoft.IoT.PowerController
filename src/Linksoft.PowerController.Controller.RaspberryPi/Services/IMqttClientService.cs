namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// MQTT client service for communicating with HostAgents.
/// </summary>
public interface IMqttClientService : IHostedService
{
    /// <summary>
    /// Whether the MQTT client is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Sends a shutdown request to a HostAgent via MQTT.
    /// </summary>
    Task SendShutdownRequestAsync(
        string hostname,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests system info from a HostAgent via MQTT.
    /// </summary>
    Task<HostAgentSystemInfo?> RequestInfoAsync(
        string hostname,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}