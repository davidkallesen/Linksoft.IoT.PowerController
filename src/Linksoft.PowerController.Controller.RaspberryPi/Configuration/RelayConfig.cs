namespace Linksoft.PowerController.Controller.RaspberryPi.Configuration;

public sealed partial class RelayConfig
{
    /// <summary>
    /// Relay identifier (1, 2, 3...).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// GPIO pin number for the relay.
    /// </summary>
    public int GpioPin { get; set; }

    /// <summary>
    /// Whether the relay is active-low (true) or active-high (false).
    /// </summary>
    public bool ActiveLow { get; set; } = true;

    /// <summary>
    /// Optional description (e.g., "Main power strip").
    /// </summary>
    public string? Description { get; set; }
}