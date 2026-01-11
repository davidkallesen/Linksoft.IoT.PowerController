namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Service for discovering devices on the network.
/// </summary>
public interface IDeviceDiscoveryService
{
    /// <summary>
    /// Event raised when scan progress is updated.
    /// </summary>
    event EventHandler<IPScannerProgressReport>? ProgressReporting;

    /// <summary>
    /// Discovers devices in the specified CIDR range.
    /// </summary>
    /// <param name="cidrRange">The CIDR range to scan (e.g., "192.168.1.0/24").</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of discovered devices.</returns>
    Task<IReadOnlyList<ScannedDevice>> DiscoverDevicesAsync(
        string cidrRange,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers devices in a range of IP addresses.
    /// </summary>
    /// <param name="startIpAddress">The starting IP address.</param>
    /// <param name="endIpAddress">The ending IP address.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of discovered devices.</returns>
    Task<IReadOnlyList<ScannedDevice>> DiscoverDevicesInRangeAsync(
        string startIpAddress,
        string endIpAddress,
        CancellationToken cancellationToken = default);
}