namespace Linksoft.PowerController.HostAgent.ApiHandlers;

/// <summary>
/// Handler business logic for the SetShutdown operation.
/// </summary>
public sealed class SetShutdownHandler : ISetShutdownHandler
{
    private readonly ILogger<SetShutdownHandler> logger;
    private readonly ISystemService systemService;

    public SetShutdownHandler(
        ILogger<SetShutdownHandler> logger,
        ISystemService systemService)
    {
        this.logger = logger;
        this.systemService = systemService;
    }

    public async Task<SetShutdownResult> ExecuteAsync(
        SetShutdownParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var request = parameters.Request;
        var mode = request?.Mode ?? ShutdownMode.Immediate;

        logger.LogInformation(
            "SetShutdown requested: Mode={Mode}, DelaySeconds={DelaySeconds}, ScheduledAt={ScheduledAt}",
            mode,
            request?.DelaySeconds,
            request?.ScheduledAt);

        if (systemService.IsShutdownInProgress)
        {
            logger.LogWarning("SetShutdown rejected: Shutdown already in progress");
            return SetShutdownResult.Conflict(new Error(
                Code: 409,
                Message: "Shutdown already in progress"));
        }

        var delaySeconds = request?.DelaySeconds;
        DateTimeOffset? scheduledAt = request?.ScheduledAt;

        var response = await systemService
            .InitiateShutdownAsync(mode, delaySeconds, scheduledAt, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "SetShutdown accepted: ScheduledAt={ScheduledAt}",
            response.ShutdownScheduledAt);

        return SetShutdownResult.Accepted(response);
    }
}