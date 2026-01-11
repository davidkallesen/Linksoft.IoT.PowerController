namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Service for controlling GPIO relays.
/// </summary>
public interface IRelayService : IDisposable
{
    /// <summary>
    /// Gets all configured relays.
    /// </summary>
    IReadOnlyList<Relay> Relays { get; }

    /// <summary>
    /// Gets a relay by ID.
    /// </summary>
    Relay? GetRelay(int relayId);

    /// <summary>
    /// Checks if a relay is currently active.
    /// </summary>
    bool IsRelayActive(int relayId);

    /// <summary>
    /// Activates a relay (cuts power).
    /// </summary>
    Task ActivateRelayAsync(
        int relayId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a relay (restores power).
    /// </summary>
    Task DeactivateRelayAsync(
        int relayId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates all relays (safety reset).
    /// </summary>
    Task DeactivateAllRelaysAsync(CancellationToken cancellationToken = default);
}