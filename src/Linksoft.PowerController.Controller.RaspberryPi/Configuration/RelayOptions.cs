namespace Linksoft.PowerController.Controller.RaspberryPi.Configuration;

[OptionsBinding("Relay", ValidateOnStart = true)]
public sealed partial class RelayOptions
{
    /// <summary>
    /// List of configured relays.
    /// </summary>
    public ICollection<RelayConfig> Relays { get; } = [];
}