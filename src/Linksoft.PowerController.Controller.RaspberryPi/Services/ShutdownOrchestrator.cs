namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Orchestrates the shutdown sequence for all devices.
/// </summary>
[Registration(Lifetime.Singleton, As = typeof(IShutdownOrchestrator))]
public sealed class ShutdownOrchestrator : IShutdownOrchestrator
{
    private const int StateLockTimeoutInMs = 30_000;
    private const int PingTimeoutMs = 3000;

    private readonly SemaphoreSlim stateLock = new(1, 1);
    private readonly IDeviceRepository deviceRepository;
    private readonly IDeviceStatusService deviceStatusService;
    private readonly IRelayService relayService;
    private readonly ShutdownOptions options;
    private readonly ILogger<ShutdownOrchestrator> logger;
    private CancellationTokenSource? shutdownCts;
    private Task? shutdownTask;

    public ShutdownOrchestrator(
        IDeviceRepository deviceRepository,
        IDeviceStatusService deviceStatusService,
        IRelayService relayService,
        IOptions<ShutdownOptions> options,
        ILogger<ShutdownOrchestrator> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.deviceRepository = deviceRepository;
        this.deviceStatusService = deviceStatusService;
        this.relayService = relayService;
        this.options = options.Value;
        this.logger = logger;
    }

    public Domain.ShutdownProgressEntity CurrentProgress { get; private set; } = new();

    public async Task<Domain.ShutdownProgressEntity> InitiateAsync(
        int delaySeconds = 0,
        CancellationToken cancellationToken = default)
    {
        var lockAcquired = false;

        try
        {
            if (CurrentProgress.State != Domain.ShutdownState.Idle)
            {
                throw new InvalidOperationException($"Cannot initiate shutdown: current state is {CurrentProgress.State}");
            }

            lockAcquired = await stateLock
                .WaitAsync(StateLockTimeoutInMs, cancellationToken)
                .ConfigureAwait(false);

            logger.LogWarning("Initiating shutdown sequence");

            CurrentProgress = new Domain.ShutdownProgressEntity
            {
                State = Domain.ShutdownState.Initiating,
                StartedAt = DateTimeOffset.UtcNow,
            };

            shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            shutdownTask = ExecuteShutdownSequenceAsync(delaySeconds, shutdownCts.Token);

            return CurrentProgress;
        }
        finally
        {
            if (lockAcquired)
            {
                stateLock.Release();
            }
        }
    }

    public async Task<Domain.ShutdownProgressEntity> CancelAsync(
        CancellationToken cancellationToken = default)
    {
        await stateLock
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            if (!CurrentProgress.CanCancel)
            {
                throw new InvalidOperationException(
                    $"Cannot cancel shutdown: current state is {CurrentProgress.State}");
            }

            logger.LogWarning("Cancelling shutdown sequence");

            if (shutdownCts is not null)
            {
                await shutdownCts
                    .CancelAsync()
                    .ConfigureAwait(false);
            }

            CurrentProgress.State = Domain.ShutdownState.Cancelled;
            CurrentProgress.CompletedAt = DateTimeOffset.UtcNow;

            return CurrentProgress;
        }
        finally
        {
            stateLock.Release();
        }
    }

    private async Task ExecuteShutdownSequenceAsync(
        int delaySeconds,
        CancellationToken cancellationToken)
    {
        try
        {
            // Optional delay before starting
            if (delaySeconds > 0)
            {
                logger.LogInformation("Waiting {DelaySeconds} seconds before shutdown", delaySeconds);
                await Task
                    .Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken)
                    .ConfigureAwait(false);
            }

            // Phase 1: Get enabled devices and initialize progress
            var devices = await deviceRepository
                .GetEnabledDevicesOrderedAsync(cancellationToken)
                .ConfigureAwait(false);

            if (devices.Count == 0)
            {
                logger.LogWarning("No enabled devices to shutdown");
                CurrentProgress.State = Domain.ShutdownState.Idle;
                CurrentProgress.CompletedAt = DateTimeOffset.UtcNow;
                return;
            }

            CurrentProgress.DeviceStatuses = devices
                .Select(d => new Domain.DeviceShutdownStatusEntity
                {
                    DeviceId = d.Id,
                    DeviceName = d.Name,
                    RelayId = d.RelayId,
                    Phase = Domain.DeviceShutdownState.Pending,
                })
                .ToList();

            // Phase 2: Send shutdown commands to all devices
            await SendShutdownCommandsAsync(devices, cancellationToken)
                .ConfigureAwait(false);

            // Phase 3: Monitor devices until all are powered off
            CurrentProgress.State = Domain.ShutdownState.Monitoring;
            await MonitorDevicesUntilPoweredOffAsync(devices, cancellationToken)
                .ConfigureAwait(false);

            // Phase 4: Verify all devices are down and activate relays
            CurrentProgress.State = Domain.ShutdownState.ReadyForPowerCut;
            await ExecutePowerCutAsync(devices, cancellationToken)
                .ConfigureAwait(false);

            // Done
            CurrentProgress.CompletedAt = DateTimeOffset.UtcNow;
            logger.LogWarning("Shutdown sequence completed successfully");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Shutdown sequence was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Shutdown sequence failed");
            CurrentProgress.State = Domain.ShutdownState.Failed;
            CurrentProgress.ErrorMessage = ex.Message;
            CurrentProgress.CompletedAt = DateTimeOffset.UtcNow;
        }
    }

    private async Task SendShutdownCommandsAsync(
        IReadOnlyList<Domain.DeviceEntity> devices,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending shutdown commands to {Count} devices", devices.Count);

        foreach (var device in devices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var deviceStatus = CurrentProgress.DeviceStatuses
                .FirstOrDefault(s => s.DeviceId == device.Id);

            if (deviceStatus is null)
            {
                continue;
            }

            deviceStatus.Phase = Domain.DeviceShutdownState.CommandSent;
            deviceStatus.CommandSentAt = DateTimeOffset.UtcNow;

            logger.LogInformation(
                "Sending shutdown command to {DeviceName} ({DeviceType})",
                device.Name,
                device.Type);

            var success = await deviceStatusService
                .SendShutdownAsync(device, cancellationToken)
                .ConfigureAwait(false);

            if (success)
            {
                deviceStatus.Phase = Domain.DeviceShutdownState.Acknowledged;
                deviceStatus.AcknowledgedAt = DateTimeOffset.UtcNow;
                logger.LogInformation("Shutdown acknowledged by {DeviceName}", device.Name);
            }
            else
            {
                deviceStatus.Phase = Domain.DeviceShutdownState.Failed;
                deviceStatus.Error = "Failed to send shutdown command";
                logger.LogWarning("Failed to send shutdown to {DeviceName}", device.Name);
            }
        }
    }

    private async Task MonitorDevicesUntilPoweredOffAsync(
        IReadOnlyList<Domain.DeviceEntity> devices,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Monitoring devices for power off");

        var startTime = DateTimeOffset.UtcNow;
        var maxWaitTime = TimeSpan.FromSeconds(options.MaxShutdownWaitSeconds);
        var pollInterval = TimeSpan.FromSeconds(options.StatusPollIntervalSeconds);
        var pingFailureCounts = new Dictionary<Guid, int>();

        while (!cancellationToken.IsCancellationRequested)
        {
            // Check timeout
            if (DateTimeOffset.UtcNow - startTime > maxWaitTime)
            {
                logger.LogWarning("Maximum shutdown wait time exceeded");
                break;
            }

            var allPoweredOff = true;

            foreach (var device in devices)
            {
                var deviceStatus = CurrentProgress.DeviceStatuses
                    .FirstOrDefault(s => s.DeviceId == device.Id);

                if (deviceStatus is null || deviceStatus.Phase == Domain.DeviceShutdownState.PoweredOff)
                {
                    continue;
                }

                if (deviceStatus.Phase == Domain.DeviceShutdownState.Failed)
                {
                    // Skip failed devices but they don't block the sequence
                    continue;
                }

                deviceStatus.Phase = Domain.DeviceShutdownState.WaitingForPowerOff;

                var isResponding = await deviceStatusService
                    .IsDeviceRespondingAsync(device, cancellationToken)
                    .ConfigureAwait(false);

                if (isResponding)
                {
                    allPoweredOff = false;
                    pingFailureCounts[device.Id] = 0;
                    logger.LogDebug("Device {DeviceName} is still responding", device.Name);
                }
                else
                {
                    // Increment failure count
                    pingFailureCounts.TryGetValue(device.Id, out var count);
                    pingFailureCounts[device.Id] = count + 1;

                    if (pingFailureCounts[device.Id] >= options.PingFailureThreshold)
                    {
                        deviceStatus.Phase = Domain.DeviceShutdownState.PoweredOff;
                        deviceStatus.PoweredOffAt = DateTimeOffset.UtcNow;
                        logger.LogInformation(
                            "Device {DeviceName} confirmed powered off after {Count} failed pings",
                            device.Name,
                            pingFailureCounts[device.Id]);
                    }
                    else
                    {
                        allPoweredOff = false;
                        logger.LogDebug(
                            "Device {DeviceName} not responding ({Count}/{Threshold})",
                            device.Name,
                            pingFailureCounts[device.Id],
                            options.PingFailureThreshold);
                    }
                }
            }

            if (allPoweredOff)
            {
                logger.LogInformation("All devices confirmed powered off");
                break;
            }

            await Task
                .Delay(pollInterval, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task ExecutePowerCutAsync(
        IReadOnlyList<Domain.DeviceEntity> devices,
        CancellationToken cancellationToken)
    {
        logger.LogWarning("Executing power cut phase");

        // Get distinct relay IDs
        var relayIds = devices
            .Select(d => d.RelayId)
            .Distinct()
            .ToList();

        foreach (var relayId in relayIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get all enabled devices for this relay
            var relayDevices = await deviceRepository
                .GetDevicesByRelayIdAsync(relayId, cancellationToken)
                .ConfigureAwait(false);

            // SAFETY CHECK: Verify ALL devices on this relay are powered off
            var allPoweredOff = true;
            foreach (var device in relayDevices)
            {
                // Final ping verification using Atc.Network PingHelper
                var ipAddress = IPAddress.Parse(device.IpAddress);
                var pingResult = await PingHelper
                    .GetStatus(ipAddress, PingTimeoutMs)
                    .ConfigureAwait(false);

                if (pingResult.Status == IPStatus.Success)
                {
                    logger.LogCritical(
                        "SAFETY ABORT: Device {DeviceName} ({IpAddress}) on relay {RelayId} " +
                        "is still responding to ping. Power cut NOT executed for this relay.",
                        device.Name,
                        device.IpAddress,
                        relayId);
                    allPoweredOff = false;
                    break;
                }
            }

            if (allPoweredOff)
            {
                logger.LogWarning(
                    "All devices on relay {RelayId} verified powered off. Activating relay.",
                    relayId);

                await relayService
                    .ActivateRelayAsync(relayId, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                logger.LogWarning(
                    "Skipping relay {RelayId} due to safety check failure",
                    relayId);
            }
        }

        CurrentProgress.State = Domain.ShutdownState.PowerCutExecuted;
    }
}