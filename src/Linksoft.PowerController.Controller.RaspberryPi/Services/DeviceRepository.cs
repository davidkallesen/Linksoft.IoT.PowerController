namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// JSON file-based repository for device configurations.
/// </summary>
[Registration(Lifetime.Singleton, As = typeof(IDeviceRepository))]
public sealed class DeviceRepository : IDeviceRepository
{
    private const int FileLockTimeoutInMs = 30_000;
    private readonly SemaphoreSlim fileLock = new(1, 1);
    private readonly string filePath;
    private readonly ILogger<DeviceRepository> logger;
    private List<Domain.DeviceEntity> devices = [];
    private bool isLoaded;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public DeviceRepository(
        IOptions<AppOptions> options,
        ILogger<DeviceRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        filePath = options.Value.DevicesFilePath;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<Domain.DeviceEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return devices.AsReadOnly();
    }

    public async Task<Domain.DeviceEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return devices.Find(d => d.Id == id);
    }

    public async Task<Domain.DeviceEntity> CreateAsync(
        Domain.DeviceEntity device,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        var lockAcquired = false;

        try
        {
            lockAcquired = await fileLock
                .WaitAsync(FileLockTimeoutInMs, cancellationToken)
                .ConfigureAwait(false);

            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            device.Id = Guid.NewGuid();
            ValidateDevice(device);

            devices.Add(device);
            await SaveAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Created device {DeviceName} ({DeviceId})", device.Name, device.Id);
            return device;
        }
        finally
        {
            if (lockAcquired)
            {
                fileLock.Release();
            }
        }
    }

    public async Task<Domain.DeviceEntity?> UpdateAsync(
        Domain.DeviceEntity device,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        var lockAcquired = false;

        try
        {
            lockAcquired = await fileLock
                .WaitAsync(FileLockTimeoutInMs, cancellationToken)
                .ConfigureAwait(false);

            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            var index = devices.FindIndex(d => d.Id == device.Id);
            if (index < 0)
            {
                return null;
            }

            ValidateDevice(device);

            devices[index] = device;
            await SaveAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Updated device {DeviceName} ({DeviceId})", device.Name, device.Id);
            return device;
        }
        finally
        {
            if (lockAcquired)
            {
                fileLock.Release();
            }
        }
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var lockAcquired = false;

        try
        {
            lockAcquired = await fileLock
                .WaitAsync(FileLockTimeoutInMs, cancellationToken)
                .ConfigureAwait(false);

            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            var device = devices.Find(d => d.Id == id);
            if (device is null)
            {
                return false;
            }

            devices.Remove(device);
            await SaveAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Deleted device {DeviceName} ({DeviceId})", device.Name, id);
            return true;
        }
        finally
        {
            if (lockAcquired)
            {
                fileLock.Release();
            }
        }
    }

    public async Task<IReadOnlyList<Domain.DeviceEntity>> GetEnabledDevicesOrderedAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return devices
            .Where(d => d.Enabled)
            .OrderBy(d => d.ShutdownOrder)
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<Domain.DeviceEntity>> GetDevicesByRelayIdAsync(
        int relayId,
        CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return devices
            .Where(d => d.RelayId == relayId && d.Enabled)
            .ToList()
            .AsReadOnly();
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (isLoaded)
        {
            return;
        }

        var lockAcquired = false;

        try
        {
            if (isLoaded)
            {
                return;
            }

            lockAcquired = await fileLock
                .WaitAsync(FileLockTimeoutInMs, cancellationToken)
                .ConfigureAwait(false);

            if (File.Exists(filePath))
            {
                var json = await File
                    .ReadAllTextAsync(filePath, cancellationToken)
                    .ConfigureAwait(false);

                var data = JsonSerializer.Deserialize<DevicesFile>(json, JsonOptions);
                devices = data?.Devices ?? [];

                logger.LogInformation("Loaded {Count} devices from {FilePath}", devices.Count, filePath);
            }
            else
            {
                devices = [];
                logger.LogInformation("No devices file found at {FilePath}, starting with empty list", filePath);
            }

            isLoaded = true;
        }
        finally
        {
            if (lockAcquired)
            {
                fileLock.Release();
            }
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data = new DevicesFile { Devices = devices };
        var json = JsonSerializer.Serialize(data, JsonOptions);

        await File
            .WriteAllTextAsync(filePath, json, cancellationToken)
            .ConfigureAwait(false);

        logger.LogDebug("Saved {Count} devices to {FilePath}", devices.Count, filePath);
    }

    private static void ValidateDevice(Domain.DeviceEntity device)
    {
        if (string.IsNullOrWhiteSpace(device.Name))
        {
            throw new ArgumentException("Device name is required", nameof(device));
        }

        if (string.IsNullOrWhiteSpace(device.IpAddress))
        {
            throw new ArgumentException("Device IP address is required", nameof(device));
        }

        if (!IPv4AddressHelper.IsValid(device.IpAddress))
        {
            throw new ArgumentException($"Invalid IPv4 address: {device.IpAddress}", nameof(device));
        }

        if (device is { Type: Domain.DeviceType.HostAgent, EndpointType: null })
        {
            throw new ArgumentException("EndpointType is required for HostAgent devices", nameof(device));
        }
    }
}