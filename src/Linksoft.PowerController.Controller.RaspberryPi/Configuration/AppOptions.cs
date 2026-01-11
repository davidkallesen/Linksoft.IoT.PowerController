namespace Linksoft.PowerController.Controller.RaspberryPi.Configuration;

[OptionsBinding("App", ValidateOnStart = true)]
public sealed partial class AppOptions
{
    /// <summary>
    /// Path to the JSON file storing device configurations.
    /// </summary>
    public string DevicesFilePath { get; set; } = "data/devices.json";
}