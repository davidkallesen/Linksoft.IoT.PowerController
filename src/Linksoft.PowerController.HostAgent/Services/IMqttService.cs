namespace Linksoft.PowerController.HostAgent.Services;

/// <summary>
/// Interface for MQTT messaging service.
/// </summary>
public interface IMqttService : IHostedService
{
    /// <summary>
    /// Gets a value indicating whether the MQTT client is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Publishes the current system status.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishStatusAsync(CancellationToken cancellationToken = default);
}