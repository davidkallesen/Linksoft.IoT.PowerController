namespace Linksoft.PowerController.Controller.RaspberryPi.ApiHandlers;

/// <summary>
/// Handler for DELETE /devices/{id} endpoint.
/// </summary>
public sealed class DeleteDeviceHandler : IDeleteDeviceHandler
{
    private readonly IDeviceRepository deviceRepository;
    private readonly ILogger<DeleteDeviceHandler> logger;

    public DeleteDeviceHandler(
        IDeviceRepository deviceRepository,
        ILogger<DeleteDeviceHandler> logger)
    {
        this.deviceRepository = deviceRepository;
        this.logger = logger;
    }

    public async Task<DeleteDeviceResult> ExecuteAsync(
        DeleteDeviceParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        logger.LogInformation("Deleting device {DeviceId}", parameters.Id);

        var deleted = await deviceRepository
            .DeleteAsync(parameters.Id, cancellationToken)
            .ConfigureAwait(false);

        if (!deleted)
        {
            return DeleteDeviceResult.NotFound(new Error(404, $"Device {parameters.Id} not found"));
        }

        return DeleteDeviceResult.NoContent();
    }
}