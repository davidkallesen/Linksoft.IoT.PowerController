namespace Linksoft.PowerController.Controller.RaspberryPi.ApiHandlers;

/// <summary>
/// Handler for PUT /devices/{id} endpoint.
/// </summary>
public sealed class UpdateDeviceHandler : IUpdateDeviceHandler
{
    private readonly IDeviceRepository deviceRepository;
    private readonly ILogger<UpdateDeviceHandler> logger;

    public UpdateDeviceHandler(
        IDeviceRepository deviceRepository,
        ILogger<UpdateDeviceHandler> logger)
    {
        this.deviceRepository = deviceRepository;
        this.logger = logger;
    }

    public async Task<UpdateDeviceResult> ExecuteAsync(
        UpdateDeviceParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        logger.LogInformation("Updating device {DeviceId}", parameters.Id);

        var existing = await deviceRepository
            .GetByIdAsync(parameters.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return UpdateDeviceResult.NotFound(new Error(404, $"Device {parameters.Id} not found"));
        }

        // Apply updates from the request
        if (!string.IsNullOrEmpty(parameters.Request.Name))
        {
            existing.Name = parameters.Request.Name;
        }

        if (!string.IsNullOrEmpty(parameters.Request.IpAddress))
        {
            existing.IpAddress = parameters.Request.IpAddress;
        }

        existing.Type = TypeMapper.ToDomainDeviceType(parameters.Request.Type);
        existing.EndpointType = TypeMapper.ToDomainEndpointType(parameters.Request.EndpointType);
        existing.Port = parameters.Request.Port;
        existing.ShutdownOrder = parameters.Request.ShutdownOrder;
        existing.RelayId = parameters.Request.RelayId;
        existing.Enabled = parameters.Request.Enabled;

        var updated = await deviceRepository
            .UpdateAsync(existing, cancellationToken)
            .ConfigureAwait(false);

        if (updated is null)
        {
            return UpdateDeviceResult.NotFound(new Error(404, $"Device {parameters.Id} not found"));
        }

        return UpdateDeviceResult.Ok(TypeMapper.ToApiDevice(updated));
    }
}