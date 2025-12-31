namespace Linksoft.PowerController.HostAgent.ApiHandlers;

/// <summary>
/// Handler business logic for the SetShutdown operation.
/// </summary>
public sealed class SetShutdownHandler : ISetShutdownHandler
{
    private readonly ISystemService systemService;
    private readonly ILogger<SetShutdownHandler> logger;

    public SetShutdownHandler(ISystemService systemService, ILogger<SetShutdownHandler> logger)
    {
        this.systemService = systemService;
        this.logger = logger;
    }

    public async Task<SetShutdownResult> ExecuteAsync(
        SetShutdownParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var request = parameters.Request;
        var mode = request?.Mode ?? ShutdownMode.Immediate;

        logger.LogInformation("SetShutdown requested: Mode={Mode}, DelaySeconds={DelaySeconds}, ScheduledAt={ScheduledAt}",
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

        logger.LogInformation("SetShutdown accepted: ScheduledAt={ScheduledAt}", response.ShutdownScheduledAt);

        return SetShutdownResult.Accepted(response);
    }
}