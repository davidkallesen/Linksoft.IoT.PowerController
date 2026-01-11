namespace Linksoft.PowerController.Controller.RaspberryPi.Models;

/// <summary>
/// Represents a relay device and its runtime state.
/// </summary>
public sealed class Relay
{
    /// <summary>
    /// Relay identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// GPIO pin number for the relay.
    /// </summary>
    public int GpioPin { get; set; }

    /// <summary>
    /// Whether the relay is active-low.
    /// </summary>
    public bool ActiveLow { get; set; } = true;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the relay is currently activated.
    /// </summary>
    public bool IsActive { get; set; }
}