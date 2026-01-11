namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// HTTP client for communicating with HostAgent REST API.
/// </summary>
public interface IHostAgentHttpClient
{
    /// <summary>
    /// Gets system info from a HostAgent.
    /// </summary>
    Task<HostAgentSystemInfo?> GetSystemInfoAsync(
        Domain.DeviceEntity device,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a shutdown request to a HostAgent.
    /// </summary>
    Task<HostAgentShutdownResponse?> SendShutdownAsync(
        Domain.DeviceEntity device,
        CancellationToken cancellationToken = default);
}