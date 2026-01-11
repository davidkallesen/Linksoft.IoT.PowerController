namespace Linksoft.PowerController.Controller.RaspberryPi.ApiHandlers;

/// <summary>
/// Handler for GET /shutdown/status endpoint.
/// </summary>
public sealed class GetShutdownStatusHandler : IGetShutdownStatusHandler
{
    private readonly IShutdownOrchestrator shutdownOrchestrator;
    private readonly ILogger<GetShutdownStatusHandler> logger;

    public GetShutdownStatusHandler(
        IShutdownOrchestrator shutdownOrchestrator,
        ILogger<GetShutdownStatusHandler> logger)
    {
        this.shutdownOrchestrator = shutdownOrchestrator;
        this.logger = logger;
    }

    public Task<GetShutdownStatusResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting shutdown status");

        var progress = shutdownOrchestrator.CurrentProgress;
        var apiProgress = TypeMapper.ToApiShutdownProgress(progress);

        return Task.FromResult(GetShutdownStatusResult.Ok(apiProgress));
    }
}