namespace Linksoft.PowerController.Controller.RaspberryPi.ApiHandlers;

/// <summary>
/// Handler for GET /devices/{id}/status endpoint.
/// </summary>
public sealed class GetDeviceStatusHandler : IGetDeviceStatusHandler
{
    private readonly IDeviceRepository deviceRepository;
    private readonly IDeviceStatusService deviceStatusService;
    private readonly ILogger<GetDeviceStatusHandler> logger;

    public GetDeviceStatusHandler(
        IDeviceRepository deviceRepository,
        IDeviceStatusService deviceStatusService,
        ILogger<GetDeviceStatusHandler> logger)
    {
        this.deviceRepository = deviceRepository;
        this.deviceStatusService = deviceStatusService;
        this.logger = logger;
    }

    public async Task<GetDeviceStatusResult> ExecuteAsync(
        GetDeviceStatusParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        logger.LogDebug("Getting status for device {DeviceId}", parameters.Id);

        var device = await deviceRepository
            .GetByIdAsync(parameters.Id, cancellationToken)
            .ConfigureAwait(false);

        if (device is null)
        {
            return GetDeviceStatusResult.NotFound(new Error(404, $"Device {parameters.Id} not found"));
        }

        var status = await deviceStatusService
            .GetDeviceStatusAsync(device, cancellationToken)
            .ConfigureAwait(false);

        return GetDeviceStatusResult.Ok(TypeMapper.ToApiDeviceStatus(status));
    }
}