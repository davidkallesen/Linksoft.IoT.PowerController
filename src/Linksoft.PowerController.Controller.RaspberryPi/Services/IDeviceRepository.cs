namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Repository for managing device configurations.
/// </summary>
public interface IDeviceRepository
{
    /// <summary>
    /// Gets all devices.
    /// </summary>
    Task<IReadOnlyList<Domain.DeviceEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a device by ID.
    /// </summary>
    Task<Domain.DeviceEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new device.
    /// </summary>
    Task<Domain.DeviceEntity> CreateAsync(
        Domain.DeviceEntity device,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing device.
    /// </summary>
    Task<Domain.DeviceEntity?> UpdateAsync(
        Domain.DeviceEntity device,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a device by ID.
    /// </summary>
    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled devices ordered by shutdown order.
    /// </summary>
    Task<IReadOnlyList<Domain.DeviceEntity>> GetEnabledDevicesOrderedAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all devices mapped to a specific relay.
    /// </summary>
    Task<IReadOnlyList<Domain.DeviceEntity>> GetDevicesByRelayIdAsync(
        int relayId,
        CancellationToken cancellationToken = default);
}