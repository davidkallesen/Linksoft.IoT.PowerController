namespace Linksoft.PowerController.Controller.RaspberryPi.Domain;

/// <summary>
/// Internal domain model for a managed device.
/// </summary>
public sealed class DeviceEntity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string IpAddress { get; set; }

    public DeviceType Type { get; set; }

    public EndpointType? EndpointType { get; set; }

    public int Port { get; set; } = 5000;

    public int ShutdownOrder { get; set; }

    public int RelayId { get; set; } = 1;

    public bool Enabled { get; set; } = true;
}