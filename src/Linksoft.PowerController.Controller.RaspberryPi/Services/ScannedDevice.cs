namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Represents a discovered device on the network (internal scan result).
/// </summary>
/// <param name="IpAddress">The IP address of the device.</param>
/// <param name="Hostname">The resolved hostname, if available.</param>
/// <param name="MacAddress">The MAC address, if available.</param>
/// <param name="MacVendor">The MAC vendor name, if available.</param>
/// <param name="OpenPorts">A list of open ports discovered on the device.</param>
public sealed record ScannedDevice(
    string IpAddress,
    string? Hostname,
    string? MacAddress,
    string? MacVendor,
    IReadOnlyList<ushort> OpenPorts);