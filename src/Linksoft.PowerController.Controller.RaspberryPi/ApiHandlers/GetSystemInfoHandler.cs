namespace Linksoft.PowerController.Controller.RaspberryPi.ApiHandlers;

/// <summary>
/// Handler for GET /system/info endpoint.
/// </summary>
public sealed class GetSystemInfoHandler : IGetSystemInfoHandler
{
    private readonly ISystemService systemService;
    private readonly ILogger<GetSystemInfoHandler> logger;

    public GetSystemInfoHandler(
        ISystemService systemService,
        ILogger<GetSystemInfoHandler> logger)
    {
        this.systemService = systemService;
        this.logger = logger;
    }

    public Task<GetSystemInfoResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting system info");

        var info = systemService.GetSystemInfo();

        return Task.FromResult(GetSystemInfoResult.Ok(info));
    }
}