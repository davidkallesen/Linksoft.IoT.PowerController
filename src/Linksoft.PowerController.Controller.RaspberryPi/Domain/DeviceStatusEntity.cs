namespace Linksoft.PowerController.Controller.RaspberryPi.Domain;

/// <summary>
/// Internal domain model for device runtime status.
/// </summary>
public sealed class DeviceStatusEntity
{
    public Guid DeviceId { get; set; }

    required public string DeviceName { get; set; }

    public DeviceConnectionState ConnectionState { get; set; }

    public bool ShutdownInProgress { get; set; }

    public DateTimeOffset? ShutdownScheduledAt { get; set; }

    public string? ServiceUptime { get; set; }

    public string? ServerUptime { get; set; }

    public string? Hostname { get; set; }

    public string? OperatingSystem { get; set; }

    public DateTimeOffset LastChecked { get; set; }

    public string? LastError { get; set; }
}