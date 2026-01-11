namespace Linksoft.PowerController.Controller.RaspberryPi.ApiHandlers;

/// <summary>
/// Handler for POST /shutdown endpoint.
/// </summary>
public sealed class InitiateShutdownHandler : IInitiateShutdownHandler
{
    private readonly IShutdownOrchestrator shutdownOrchestrator;
    private readonly ILogger<InitiateShutdownHandler> logger;

    public InitiateShutdownHandler(
        IShutdownOrchestrator shutdownOrchestrator,
        ILogger<InitiateShutdownHandler> logger)
    {
        this.shutdownOrchestrator = shutdownOrchestrator;
        this.logger = logger;
    }

    public async Task<InitiateShutdownResult> ExecuteAsync(
        InitiateShutdownParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var delaySeconds = parameters.Request?.DelaySeconds ?? 0;

        logger.LogWarning("Initiating shutdown with delay {DelaySeconds}s", delaySeconds);

        try
        {
            var progress = await shutdownOrchestrator
                .InitiateAsync(delaySeconds, cancellationToken)
                .ConfigureAwait(false);

            return InitiateShutdownResult.Accepted(TypeMapper.ToApiShutdownProgress(progress));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Cannot initiate shutdown");
            return InitiateShutdownResult.Conflict(new Error(409, ex.Message));
        }
    }
}