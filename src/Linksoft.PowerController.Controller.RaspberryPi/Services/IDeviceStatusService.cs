namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Service for monitoring device status.
/// </summary>
public interface IDeviceStatusService
{
    /// <summary>
    /// Gets the current status of a device.
    /// </summary>
    Task<Domain.DeviceStatusEntity> GetDeviceStatusAsync(
        Domain.DeviceEntity device,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of all devices.
    /// </summary>
    Task<IReadOnlyList<Domain.DeviceStatusEntity>> GetAllDeviceStatusesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a shutdown command to a device.
    /// </summary>
    Task<bool> SendShutdownAsync(
        Domain.DeviceEntity device,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a device is still responding (API or ping).
    /// </summary>
    Task<bool> IsDeviceRespondingAsync(
        Domain.DeviceEntity device,
        CancellationToken cancellationToken = default);
}