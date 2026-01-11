namespace Linksoft.PowerController.Controller.RaspberryPi.ApiHandlers;

/// <summary>
/// Handler for GET /devices endpoint.
/// </summary>
public sealed class GetDevicesHandler : IGetDevicesHandler
{
    private readonly IDeviceRepository deviceRepository;
    private readonly ILogger<GetDevicesHandler> logger;

    public GetDevicesHandler(
        IDeviceRepository deviceRepository,
        ILogger<GetDevicesHandler> logger)
    {
        this.deviceRepository = deviceRepository;
        this.logger = logger;
    }

    public async Task<GetDevicesResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting all devices");

        var devices = await deviceRepository
            .GetAllAsync(cancellationToken)
            .ConfigureAwait(false);

        var apiDevices = devices
            .Select(TypeMapper.ToApiDevice)
            .ToArray();

        return GetDevicesResult.Ok(apiDevices);
    }
}