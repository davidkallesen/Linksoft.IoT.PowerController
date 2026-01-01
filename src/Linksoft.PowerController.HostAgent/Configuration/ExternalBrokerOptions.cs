namespace Linksoft.PowerController.HostAgent.Configuration;

/// <summary>
/// Configuration options for connecting to an external MQTT broker.
/// </summary>
public sealed class ExternalBrokerOptions
{
    /// <summary>
    /// Gets or sets the broker hostname or IP address.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the broker port.
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// Gets or sets a value indicating whether to use TLS.
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    public string? Password { get; set; }
}