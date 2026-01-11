namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Service for monitoring device status.
/// </summary>
[Registration(Lifetime.Singleton, As = typeof(IDeviceStatusService))]
public sealed class DeviceStatusService : IDeviceStatusService
{
    private const int PingTimeoutMs = 3000;

    private readonly IDeviceRepository deviceRepository;
    private readonly IHostAgentHttpClient hostAgentHttpClient;
    private readonly IHostnameResolver hostnameResolver;
    private readonly IMqttClientService? mqttClientService;
    private readonly ILogger<DeviceStatusService> logger;

    public DeviceStatusService(
        IDeviceRepository deviceRepository,
        IHostAgentHttpClient hostAgentHttpClient,
        IHostnameResolver hostnameResolver,
        ILogger<DeviceStatusService> logger,
        IMqttClientService? mqttClientService = null)
    {
        this.deviceRepository = deviceRepository;
        this.hostAgentHttpClient = hostAgentHttpClient;
        this.hostnameResolver = hostnameResolver;
        this.mqttClientService = mqttClientService;
        this.logger = logger;
    }

    public async Task<Domain.DeviceStatusEntity> GetDeviceStatusAsync(
        Domain.DeviceEntity device,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        var status = new Domain.DeviceStatusEntity
        {
            DeviceId = device.Id,
            DeviceName = device.Name,
            LastChecked = DateTimeOffset.UtcNow,
        };

        try
        {
            HostAgentSystemInfo? info = null;

            if (device.Type == Domain.DeviceType.HostAgent)
            {
                if (device.EndpointType == Domain.EndpointType.RestApi)
                {
                    info = await hostAgentHttpClient
                        .GetSystemInfoAsync(device, cancellationToken)
                        .ConfigureAwait(false);
                }
                else if (device.EndpointType == Domain.EndpointType.Mqtt && mqttClientService is not null)
                {
                    info = await mqttClientService
                        .RequestInfoAsync(device.Name, TimeSpan.FromSeconds(5), cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            if (info is not null)
            {
                status.ConnectionState = info.ShutdownInProgress
                    ? Domain.DeviceConnectionState.ShuttingDown
                    : Domain.DeviceConnectionState.Online;
                status.ShutdownInProgress = info.ShutdownInProgress;
                status.ShutdownScheduledAt = info.ShutdownScheduledAt;
                status.ServiceUptime = info.ServiceUptime;
                status.ServerUptime = info.ServerUptime;
                status.Hostname = info.Hostname;
                status.OperatingSystem = info.OperatingSystem;
            }
            else
            {
                // API failed, try ping using Atc.Network PingHelper
                var ipAddress = IPAddress.Parse(device.IpAddress);
                var pingResult = await PingHelper
                    .GetStatus(ipAddress, PingTimeoutMs)
                    .ConfigureAwait(false);

                if (pingResult.Status == IPStatus.Success)
                {
                    status.ConnectionState = Domain.DeviceConnectionState.NotResponding;

                    // Try to resolve hostname since device is reachable
                    status.Hostname = await hostnameResolver
                        .ResolveHostnameAsync(device.IpAddress, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    status.ConnectionState = Domain.DeviceConnectionState.PoweredOff;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error getting status for device {DeviceName}", device.Name);
            status.ConnectionState = Domain.DeviceConnectionState.Unknown;
            status.LastError = ex.Message;
        }

        return status;
    }

    public async Task<IReadOnlyList<Domain.DeviceStatusEntity>> GetAllDeviceStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        var devices = await deviceRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        var tasks = devices.Select(d => GetDeviceStatusAsync(d, cancellationToken));
        var statuses = await Task.WhenAll(tasks).ConfigureAwait(false);

        return statuses
            .ToList()
            .AsReadOnly();
    }

    public async Task<bool> SendShutdownAsync(
        Domain.DeviceEntity device,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        try
        {
            switch (device.Type)
            {
                case Domain.DeviceType.HostAgent when device.EndpointType == Domain.EndpointType.RestApi:
                    var response = await hostAgentHttpClient
                        .SendShutdownAsync(device, cancellationToken)
                        .ConfigureAwait(false);
                    return response is not null;

                case Domain.DeviceType.HostAgent when device.EndpointType == Domain.EndpointType.Mqtt:
                    if (mqttClientService is null)
                    {
                        logger.LogWarning(
                            "MQTT client not available for device {DeviceName}",
                            device.Name);
                        return false;
                    }

                    await mqttClientService
                        .SendShutdownRequestAsync(device.Name, cancellationToken)
                        .ConfigureAwait(false);
                    return true;

                case Domain.DeviceType.EsuEcos50210:
                    // Placeholder: ESU Ecos shutdown not implemented
                    logger.LogWarning(
                        "ESU Ecos 50210 shutdown not implemented for device {DeviceName}",
                        device.Name);
                    return true; // Return true to continue the sequence

                default:
                    logger.LogWarning(
                        "Unknown device type {DeviceType} for device {DeviceName}",
                        device.Type,
                        device.Name);
                    return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send shutdown to device {DeviceName}",
                device.Name);
            return false;
        }
    }

    public async Task<bool> IsDeviceRespondingAsync(
        Domain.DeviceEntity device,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        // First try API
        if (device.Type == Domain.DeviceType.HostAgent && device.EndpointType == Domain.EndpointType.RestApi)
        {
            var info = await hostAgentHttpClient
                .GetSystemInfoAsync(device, cancellationToken)
                .ConfigureAwait(false);

            if (info is not null)
            {
                return true;
            }
        }

        // Fall back to ping using Atc.Network PingHelper
        var ipAddress = IPAddress.Parse(device.IpAddress);
        var pingResult = await PingHelper
            .GetStatus(ipAddress, PingTimeoutMs)
            .ConfigureAwait(false);

        return pingResult.Status == IPStatus.Success;
    }
}