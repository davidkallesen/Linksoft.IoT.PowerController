namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Service for discovering devices on the network using Atc.Network.
/// </summary>
[Registration(Lifetime.Singleton, As = typeof(IDeviceDiscoveryService))]
public sealed class DeviceDiscoveryService : IDeviceDiscoveryService
{
    private readonly ILogger<DeviceDiscoveryService> logger;
    private readonly IPScannerConfig scannerConfig;

    public DeviceDiscoveryService(ILogger<DeviceDiscoveryService> logger)
    {
        this.logger = logger;

        // Configure scanner with sensible defaults for device discovery
        scannerConfig = new IPScannerConfig
        {
            IcmpPing = true,
            ResolveHostName = true,
            ResolveMacAddress = true,
            ResolveVendorFromMacAddress = true,
            PortNumbers = [80, 443, 5000, 5001, 1883], // HTTP, HTTPS, Kestrel, MQTT
        };
    }

    public event EventHandler<IPScannerProgressReport>? ProgressReporting;

    public async Task<IReadOnlyList<ScannedDevice>> DiscoverDevicesAsync(
        string cidrRange,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cidrRange);

        // Parse CIDR (e.g., "192.168.1.0/24")
        var parts = cidrRange.Split('/');
        if (parts.Length != 2)
        {
            throw new ArgumentException(
                "Invalid CIDR format. Expected format: '192.168.1.0/24'",
                nameof(cidrRange));
        }

        if (!IPAddress.TryParse(parts[0], out var ipAddress))
        {
            throw new ArgumentException(
                $"Invalid IP address in CIDR: {parts[0]}",
                nameof(cidrRange));
        }

        if (!byte.TryParse(parts[1], out var cidrLength) || cidrLength > 32)
        {
            throw new ArgumentException(
                $"Invalid CIDR length: {parts[1]}. Must be 0-32.",
                nameof(cidrRange));
        }

        logger.LogInformation(
            "Starting device discovery for CIDR range {CidrRange}",
            cidrRange);

        var scanner = new IPScanner(scannerConfig);
        scanner.ProgressReporting += OnProgressReporting;

        try
        {
            var results = await scanner
                .ScanCidrRange(ipAddress, cidrLength, cancellationToken)
                .ConfigureAwait(false);

            return ProcessScanResults(results);
        }
        finally
        {
            scanner.ProgressReporting -= OnProgressReporting;
        }
    }

    public async Task<IReadOnlyList<ScannedDevice>> DiscoverDevicesInRangeAsync(
        string startIpAddress,
        string endIpAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startIpAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(endIpAddress);

        if (!IPAddress.TryParse(startIpAddress, out var startIp))
        {
            throw new ArgumentException(
                $"Invalid start IP address: {startIpAddress}",
                nameof(startIpAddress));
        }

        if (!IPAddress.TryParse(endIpAddress, out var endIp))
        {
            throw new ArgumentException(
                $"Invalid end IP address: {endIpAddress}",
                nameof(endIpAddress));
        }

        logger.LogInformation(
            "Starting device discovery for IP range {StartIp} - {EndIp}",
            startIpAddress,
            endIpAddress);

        var scanner = new IPScanner(scannerConfig);
        scanner.ProgressReporting += OnProgressReporting;

        try
        {
            var results = await scanner
                .ScanRange(startIp, endIp, cancellationToken)
                .ConfigureAwait(false);

            return ProcessScanResults(results);
        }
        finally
        {
            scanner.ProgressReporting -= OnProgressReporting;
        }
    }

    private void OnProgressReporting(
        object? sender,
        IPScannerProgressReport e)
    {
        logger.LogDebug(
            "Discovery progress: {Percentage:F1}% - {LatestUpdate}",
            e.PercentageCompleted,
            e.LatestUpdate);

        ProgressReporting?.Invoke(this, e);
    }

    private List<ScannedDevice> ProcessScanResults(IPScanResults results)
    {
        var devices = results
            .CollectedResultsFilteredOnHasConnections
            .Select(r => new ScannedDevice(
                r.IPAddress.ToString(),
                r.Hostname,
                r.MacAddress,
                r.MacVendor,
                r.OpenPortNumbers.ToList().AsReadOnly()))
            .ToList();

        logger.LogInformation(
            "Device discovery completed. Found {Count} device(s) with connections.",
            devices.Count);

        return devices;
    }
}