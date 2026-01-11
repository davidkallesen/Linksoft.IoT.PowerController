namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Service for controlling GPIO relays.
/// </summary>
[Registration(Lifetime.Singleton, As = typeof(IRelayService))]
public sealed class RelayService : IRelayService
{
    private readonly GpioController? controller;
    private readonly List<Relay> relays = [];
    private readonly ShutdownOptions shutdownOptions;
    private readonly ILogger<RelayService> logger;
    private readonly bool isSimulated;

    public RelayService(
        IOptions<RelayOptions> relayOptions,
        IOptions<ShutdownOptions> shutdownOptions,
        ILogger<RelayService> logger)
    {
        ArgumentNullException.ThrowIfNull(relayOptions);
        ArgumentNullException.ThrowIfNull(shutdownOptions);

        this.shutdownOptions = shutdownOptions.Value;
        this.logger = logger;

        // Try to initialize GPIO controller
        try
        {
            controller = new GpioController();
            isSimulated = false;
            logger.LogInformation("GPIO controller initialized");
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "GPIO controller not available, running in simulated mode");
            isSimulated = true;
        }

        // Initialize relays from configuration
        foreach (var config in relayOptions.Value.Relays)
        {
            var relay = new Relay
            {
                Id = config.Id,
                GpioPin = config.GpioPin,
                ActiveLow = config.ActiveLow,
                Description = config.Description,
                IsActive = false,
            };

            relays.Add(relay);

            if (!isSimulated && controller is not null)
            {
                // Initialize pin in safe (inactive) state
                var initialValue = config.ActiveLow ? PinValue.High : PinValue.Low;
                controller.OpenPin(config.GpioPin, PinMode.Output, initialValue);
            }

            logger.LogInformation(
                "Relay {RelayId} initialized on GPIO pin {Pin} ({Description})",
                relay.Id,
                relay.GpioPin,
                relay.Description ?? "no description");
        }
    }

    public IReadOnlyList<Relay> Relays => relays.AsReadOnly();

    public Relay? GetRelay(int relayId)
        => relays.Find(r => r.Id == relayId);

    public bool IsRelayActive(int relayId)
    {
        var relay = GetRelay(relayId);
        return relay?.IsActive ?? false;
    }

    public async Task ActivateRelayAsync(
        int relayId,
        CancellationToken cancellationToken = default)
    {
        var relay = GetRelay(relayId);
        if (relay is null)
        {
            logger.LogWarning("Relay {RelayId} not found", relayId);
            return;
        }

        if (relay.IsActive)
        {
            logger.LogDebug("Relay {RelayId} is already active", relayId);
            return;
        }

        logger.LogWarning(
            "ACTIVATING RELAY {RelayId} on GPIO pin {Pin} - Power cut initiated",
            relayId,
            relay.GpioPin);

        if (!isSimulated && controller is not null)
        {
            var activeValue = relay.ActiveLow ? PinValue.Low : PinValue.High;
            controller.Write(relay.GpioPin, activeValue);
        }

        relay.IsActive = true;

        // Hold for configured duration
        await Task
            .Delay(shutdownOptions.RelayHoldDurationMs, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task DeactivateRelayAsync(
        int relayId,
        CancellationToken cancellationToken = default)
    {
        var relay = GetRelay(relayId);
        if (relay is null)
        {
            logger.LogWarning("Relay {RelayId} not found", relayId);
            return Task.CompletedTask;
        }

        if (!relay.IsActive)
        {
            logger.LogDebug("Relay {RelayId} is already inactive", relayId);
            return Task.CompletedTask;
        }

        logger.LogInformation(
            "Deactivating relay {RelayId} on GPIO pin {Pin}",
            relayId,
            relay.GpioPin);

        if (!isSimulated && controller is not null)
        {
            var inactiveValue = relay.ActiveLow ? PinValue.High : PinValue.Low;
            controller.Write(relay.GpioPin, inactiveValue);
        }

        relay.IsActive = false;
        return Task.CompletedTask;
    }

    public async Task DeactivateAllRelaysAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deactivating all relays");

        foreach (var relay in relays)
        {
            await DeactivateRelayAsync(relay.Id, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        // Ensure all relays are in safe state before disposing
        foreach (var relay in relays)
        {
            if (!isSimulated && controller is not null)
            {
                var inactiveValue = relay.ActiveLow ? PinValue.High : PinValue.Low;
                controller.Write(relay.GpioPin, inactiveValue);
                controller.ClosePin(relay.GpioPin);
            }

            relay.IsActive = false;
        }

        controller?.Dispose();
        logger.LogInformation("RelayService disposed, all relays in safe state");
    }
}