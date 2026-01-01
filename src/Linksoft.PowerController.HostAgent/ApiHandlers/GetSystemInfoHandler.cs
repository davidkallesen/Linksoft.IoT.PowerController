namespace Linksoft.PowerController.HostAgent.ApiHandlers;

/// <summary>
/// Handler business logic for the GetSystemInfo operation.
/// </summary>
public sealed class GetSystemInfoHandler : IGetSystemInfoHandler
{
    private readonly ILogger<GetSystemInfoHandler> logger;
    private readonly ISystemService systemService;

    public GetSystemInfoHandler(
        ILogger<GetSystemInfoHandler> logger,
        ISystemService systemService)
    {
        this.logger = logger;
        this.systemService = systemService;
    }

    public Task<GetSystemInfoResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("GetSystemInfo requested");

        var systemInfo = systemService.GetSystemInfo();

        logger.LogInformation(
            "GetSystemInfo completed: ServiceUptime={ServiceUptime}, ShutdownInProgress={ShutdownInProgress}",
            systemInfo.ServiceUptime,
            systemInfo.ShutdownInProgress);

        return Task.FromResult(GetSystemInfoResult.Ok(systemInfo));
    }
}