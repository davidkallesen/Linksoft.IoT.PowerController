namespace Linksoft.PowerController.HostAgent.Configuration;

/// <summary>
/// Configuration options for MQTT topics.
/// </summary>
public sealed class TopicOptions
{
    /// <summary>
    /// Gets or sets the base topic prefix.
    /// </summary>
    public string BaseTopic { get; set; } = "powercontroller";

    /// <summary>
    /// Gets or sets the interval in seconds for automatic status publishing.
    /// </summary>
    public int StatusInterval { get; set; } = 30;
}