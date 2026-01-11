namespace Linksoft.PowerController.Controller.RaspberryPi.Domain;

/// <summary>
/// Internal domain model for shutdown progress tracking.
/// </summary>
public sealed class ShutdownProgressEntity
{
    public ShutdownState State { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public IList<DeviceShutdownStatusEntity> DeviceStatuses { get; set; } = new List<DeviceShutdownStatusEntity>();

    public string? ErrorMessage { get; set; }

    public bool CanCancel
        => State
            is ShutdownState.Initiating
            or ShutdownState.Monitoring;
}