namespace Linksoft.PowerController.Controller.RaspberryPi.Configuration;

[OptionsBinding("Shutdown", ValidateOnStart = true)]
public sealed partial class ShutdownOptions
{
    /// <summary>
    /// Timeout in seconds to wait for each client to acknowledge shutdown.
    /// </summary>
    public int ClientAckTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Interval in seconds between status polls during monitoring phase.
    /// </summary>
    public int StatusPollIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Maximum time in seconds to wait for all clients to power off.
    /// </summary>
    public int MaxShutdownWaitSeconds { get; set; } = 300;

    /// <summary>
    /// Number of consecutive failed pings before marking client as powered off.
    /// </summary>
    public int PingFailureThreshold { get; set; } = 3;

    /// <summary>
    /// Duration in milliseconds to hold the relay active.
    /// </summary>
    public int RelayHoldDurationMs { get; set; } = 1000;
}