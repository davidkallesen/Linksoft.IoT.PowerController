namespace Linksoft.PowerController.Controller.RaspberryPi.ApiHandlers;

/// <summary>
/// Handler for POST /shutdown/cancel endpoint.
/// </summary>
public sealed class CancelShutdownHandler : ICancelShutdownHandler
{
    private readonly IShutdownOrchestrator shutdownOrchestrator;
    private readonly ILogger<CancelShutdownHandler> logger;

    public CancelShutdownHandler(
        IShutdownOrchestrator shutdownOrchestrator,
        ILogger<CancelShutdownHandler> logger)
    {
        this.shutdownOrchestrator = shutdownOrchestrator;
        this.logger = logger;
    }

    public async Task<CancelShutdownResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning("Cancelling shutdown");

        try
        {
            var progress = await shutdownOrchestrator
                .CancelAsync(cancellationToken)
                .ConfigureAwait(false);

            return CancelShutdownResult.Ok(TypeMapper.ToApiShutdownProgress(progress));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Cannot cancel shutdown");
            return CancelShutdownResult.Conflict(new Error(409, ex.Message));
        }
    }
}