namespace Linksoft.PowerController.HostAgent.Configuration;

/// <summary>
/// Configuration options for MQTT connectivity.
/// </summary>
public sealed class MqttOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether MQTT is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the broker mode: "External" or "Embedded".
    /// </summary>
    public string Mode { get; set; } = "External";

    /// <summary>
    /// Gets or sets the external broker connection options.
    /// </summary>
    public ExternalBrokerOptions External { get; set; } = new();

    /// <summary>
    /// Gets or sets the embedded broker options.
    /// </summary>
    public EmbeddedBrokerOptions Embedded { get; set; } = new();

    /// <summary>
    /// Gets or sets the topic configuration options.
    /// </summary>
    public TopicOptions Topics { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether embedded broker mode is configured.
    /// </summary>
    public bool IsEmbeddedMode
        => string.Equals(Mode, "Embedded", StringComparison.OrdinalIgnoreCase);
}