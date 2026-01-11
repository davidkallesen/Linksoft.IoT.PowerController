namespace Linksoft.PowerController.Controller.RaspberryPi.Configuration;

[OptionsBinding("Mqtt", ValidateOnStart = true)]
public sealed partial class MqttOptions
{
    /// <summary>
    /// Whether MQTT is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// MQTT broker host.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// MQTT broker port.
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// Whether to use TLS for MQTT connection.
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// Optional username for MQTT authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Optional password for MQTT authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Base topic for MQTT messages.
    /// </summary>
    public string BaseTopic { get; set; } = "powercontroller";
}