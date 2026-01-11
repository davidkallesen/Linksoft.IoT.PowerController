namespace Linksoft.PowerController.Controller.RaspberryPi.ApiHandlers;

/// <summary>
/// Handler for GET /devices/status endpoint.
/// </summary>
public sealed class GetAllDeviceStatusesHandler : IGetAllDeviceStatusesHandler
{
    private readonly IDeviceStatusService deviceStatusService;
    private readonly ILogger<GetAllDeviceStatusesHandler> logger;

    public GetAllDeviceStatusesHandler(
        IDeviceStatusService deviceStatusService,
        ILogger<GetAllDeviceStatusesHandler> logger)
    {
        this.deviceStatusService = deviceStatusService;
        this.logger = logger;
    }

    public async Task<GetAllDeviceStatusesResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting status for all devices");

        var statuses = await deviceStatusService
            .GetAllDeviceStatusesAsync(cancellationToken)
            .ConfigureAwait(false);

        var apiStatuses = statuses
            .Select(TypeMapper.ToApiDeviceStatus)
            .ToArray();

        return GetAllDeviceStatusesResult.Ok(apiStatuses);
    }
}