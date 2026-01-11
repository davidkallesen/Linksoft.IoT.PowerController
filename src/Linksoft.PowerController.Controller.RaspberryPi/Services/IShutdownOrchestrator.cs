namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Orchestrates the shutdown sequence for all devices.
/// </summary>
public interface IShutdownOrchestrator
{
    /// <summary>
    /// Gets the current shutdown progress.
    /// </summary>
    Domain.ShutdownProgressEntity CurrentProgress { get; }

    /// <summary>
    /// Initiates the shutdown sequence.
    /// </summary>
    Task<Domain.ShutdownProgressEntity> InitiateAsync(
        int delaySeconds = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the shutdown sequence if possible.
    /// </summary>
    Task<Domain.ShutdownProgressEntity> CancelAsync(
        CancellationToken cancellationToken = default);
}