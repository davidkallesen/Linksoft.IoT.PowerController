namespace Linksoft.PowerController.Controller.RaspberryPi.ApiHandlers;

/// <summary>
/// Handler for GET /devices/{id} endpoint.
/// </summary>
public sealed class GetDeviceByIdHandler : IGetDeviceByIdHandler
{
    private readonly IDeviceRepository deviceRepository;
    private readonly ILogger<GetDeviceByIdHandler> logger;

    public GetDeviceByIdHandler(
        IDeviceRepository deviceRepository,
        ILogger<GetDeviceByIdHandler> logger)
    {
        this.deviceRepository = deviceRepository;
        this.logger = logger;
    }

    public async Task<GetDeviceByIdResult> ExecuteAsync(
        GetDeviceByIdParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        logger.LogDebug("Getting device {DeviceId}", parameters.Id);

        var device = await deviceRepository
            .GetByIdAsync(parameters.Id, cancellationToken)
            .ConfigureAwait(false);

        if (device is null)
        {
            return GetDeviceByIdResult.NotFound(new Error(404, $"Device {parameters.Id} not found"));
        }

        return GetDeviceByIdResult.Ok(TypeMapper.ToApiDevice(device));
    }
}