namespace Linksoft.PowerController.HostAgent.Services;

public interface ISystemService
{
    SystemInfo GetSystemInfo();

    bool IsShutdownInProgress { get; }

    DateTimeOffset? ShutdownScheduledAt { get; }

    Task<ShutdownResponse> InitiateShutdownAsync(
        ShutdownMode mode,
        int? delaySeconds,
        DateTimeOffset? scheduledAt,
        CancellationToken cancellationToken = default);
}