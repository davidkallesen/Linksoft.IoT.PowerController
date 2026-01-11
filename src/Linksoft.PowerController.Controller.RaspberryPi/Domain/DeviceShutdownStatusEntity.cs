namespace Linksoft.PowerController.Controller.RaspberryPi.Domain;

public sealed class DeviceShutdownStatusEntity
{
    public Guid DeviceId { get; set; }

    public required string DeviceName { get; set; }

    public int RelayId { get; set; }

    public DeviceShutdownState Phase { get; set; }

    public DateTimeOffset? CommandSentAt { get; set; }

    public DateTimeOffset? AcknowledgedAt { get; set; }

    public DateTimeOffset? PoweredOffAt { get; set; }

    public string? Error { get; set; }
}