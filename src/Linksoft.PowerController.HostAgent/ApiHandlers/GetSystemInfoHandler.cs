namespace Linksoft.PowerController.HostAgent.ApiHandlers;

/// <summary>
/// Handler business logic for the GetSystemInfo operation.
/// </summary>
public sealed class GetSystemInfoHandler(
    ILogger<GetSystemInfoHandler> logger,
    ISystemService systemService)
    : IGetSystemInfoHandler
{
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