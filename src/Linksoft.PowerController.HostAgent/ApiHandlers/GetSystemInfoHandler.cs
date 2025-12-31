namespace Linksoft.PowerController.HostAgent.ApiHandlers;

/// <summary>
/// Handler business logic for the GetSystemInfo operation.
/// </summary>
public sealed class GetSystemInfoHandler : IGetSystemInfoHandler
{
    private readonly ISystemService systemService;
    private readonly ILogger<GetSystemInfoHandler> logger;

    public GetSystemInfoHandler(ISystemService systemService, ILogger<GetSystemInfoHandler> logger)
    {
        this.systemService = systemService;
        this.logger = logger;
    }

    public Task<GetSystemInfoResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("GetSystemInfo requested");

        var systemInfo = systemService.GetSystemInfo();

        logger.LogInformation("GetSystemInfo completed: ServiceUptime={ServiceUptime}, ShutdownInProgress={ShutdownInProgress}",
            systemInfo.ServiceUptime,
            systemInfo.ShutdownInProgress);

        return Task.FromResult(GetSystemInfoResult.Ok(systemInfo));
    }
}