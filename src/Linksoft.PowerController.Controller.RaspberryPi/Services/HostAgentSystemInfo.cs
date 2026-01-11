namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// System info response from HostAgent.
/// </summary>
public sealed record HostAgentSystemInfo(
    string ServiceUptime,
    string ServerUptime,
    bool ShutdownInProgress,
    DateTimeOffset? ShutdownScheduledAt,
    string OperatingSystem,
    string Hostname);