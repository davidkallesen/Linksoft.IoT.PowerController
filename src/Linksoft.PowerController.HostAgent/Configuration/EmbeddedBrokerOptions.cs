namespace Linksoft.PowerController.HostAgent.Configuration;

/// <summary>
/// Configuration options for the embedded MQTT broker.
/// </summary>
public sealed class EmbeddedBrokerOptions
{
    /// <summary>
    /// Gets or sets the port to listen on.
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// Gets or sets a value indicating whether to use TLS.
    /// </summary>
    public bool UseTls { get; set; }
}