namespace Linksoft.PowerController.Controller.RaspberryPi.ApiHandlers;

/// <summary>
/// Handler for POST /devices/discover endpoint.
/// </summary>
public sealed class DiscoverDevicesHandler : IDiscoverDevicesHandler
{
    private readonly IDeviceDiscoveryService discoveryService;
    private readonly ILogger<DiscoverDevicesHandler> logger;

    public DiscoverDevicesHandler(
        IDeviceDiscoveryService discoveryService,
        ILogger<DiscoverDevicesHandler> logger)
    {
        this.discoveryService = discoveryService;
        this.logger = logger;
    }

    public async Task<DiscoverDevicesResult> ExecuteAsync(
        DiscoverDevicesParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        try
        {
            IReadOnlyList<ScannedDevice> devices;

            if (!string.IsNullOrEmpty(parameters.Request.CidrRange))
            {
                logger.LogInformation(
                    "Starting device discovery for CIDR range: {CidrRange}",
                    parameters.Request.CidrRange);

                devices = await discoveryService
                    .DiscoverDevicesAsync(parameters.Request.CidrRange, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (!string.IsNullOrEmpty(parameters.Request.StartIpAddress) &&
                     !string.IsNullOrEmpty(parameters.Request.EndIpAddress))
            {
                logger.LogInformation(
                    "Starting device discovery for range: {StartIp} - {EndIp}",
                    parameters.Request.StartIpAddress,
                    parameters.Request.EndIpAddress);

                devices = await discoveryService
                    .DiscoverDevicesInRangeAsync(
                        parameters.Request.StartIpAddress,
                        parameters.Request.EndIpAddress,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                return DiscoverDevicesResult.BadRequest(
                    new Error(400, "Either cidrRange or both startIpAddress and endIpAddress must be provided."));
            }

            var apiDevices = devices
                .Select(d => new Linksoft.PowerController.Controller.RaspberryPi.Generated.Devices.Models.DiscoveredDevice(
                    d.IpAddress,
                    d.Hostname ?? string.Empty,
                    d.MacAddress ?? string.Empty,
                    d.MacVendor ?? string.Empty,
                    d.OpenPorts.Select(p => (int)p).ToArray()))
                .ToArray();

            logger.LogInformation("Device discovery completed. Found {Count} device(s).", apiDevices.Length);

            return DiscoverDevicesResult.Ok(apiDevices);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid discovery parameters");
            return DiscoverDevicesResult.BadRequest(new Error(400, ex.Message));
        }
    }
}