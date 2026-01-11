namespace Linksoft.PowerController.Controller.RaspberryPi.ApiHandlers;

/// <summary>
/// Handler for POST /devices endpoint.
/// </summary>
public sealed class CreateDeviceHandler : ICreateDeviceHandler
{
    private readonly IDeviceRepository deviceRepository;
    private readonly ILogger<CreateDeviceHandler> logger;

    public CreateDeviceHandler(
        IDeviceRepository deviceRepository,
        ILogger<CreateDeviceHandler> logger)
    {
        this.deviceRepository = deviceRepository;
        this.logger = logger;
    }

    public async Task<CreateDeviceResult> ExecuteAsync(
        CreateDeviceParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        logger.LogInformation("Creating device {DeviceName}", parameters.Request.Name);

        var device = new Domain.DeviceEntity
        {
            Name = parameters.Request.Name,
            IpAddress = parameters.Request.IpAddress,
            Type = TypeMapper.ToDomainDeviceType(parameters.Request.Type),
            EndpointType = TypeMapper.ToDomainEndpointType(parameters.Request.EndpointType),
            Port = parameters.Request.Port,
            ShutdownOrder = parameters.Request.ShutdownOrder,
            RelayId = parameters.Request.RelayId,
            Enabled = parameters.Request.Enabled,
        };

        try
        {
            var created = await deviceRepository
                .CreateAsync(device, cancellationToken)
                .ConfigureAwait(false);

            return CreateDeviceResult.Created(TypeMapper.ToApiDevice(created));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid device data");
            return CreateDeviceResult.BadRequest(new Error(400, ex.Message));
        }
    }
}