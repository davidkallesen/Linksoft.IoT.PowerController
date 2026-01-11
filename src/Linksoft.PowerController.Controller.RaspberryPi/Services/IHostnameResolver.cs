namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Service for resolving hostnames from IP addresses.
/// </summary>
public interface IHostnameResolver
{
    /// <summary>
    /// Resolves the hostname for a given IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address to resolve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The resolved hostname, or null if resolution fails.</returns>
    Task<string?> ResolveHostnameAsync(
        string ipAddress,
        CancellationToken cancellationToken = default);
}