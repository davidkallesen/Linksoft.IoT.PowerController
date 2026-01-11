namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Shutdown response from HostAgent.
/// </summary>
public sealed record HostAgentShutdownResponse(
    string Message,
    DateTimeOffset ShutdownScheduledAt);