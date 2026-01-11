namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Service for retrieving controller system information.
/// </summary>
[Registration(Lifetime.Singleton, As = typeof(ISystemService))]
public sealed class SystemService : ISystemService
{
    private readonly DateTimeOffset serviceStartTime = DateTimeOffset.UtcNow;
    private readonly IDeviceRepository deviceRepository;
    private readonly IShutdownOrchestrator shutdownOrchestrator;
    private readonly IMqttClientService? mqttClientService;

    public SystemService(
        IDeviceRepository deviceRepository,
        IShutdownOrchestrator shutdownOrchestrator,
        IMqttClientService? mqttClientService = null)
    {
        this.deviceRepository = deviceRepository;
        this.shutdownOrchestrator = shutdownOrchestrator;
        this.mqttClientService = mqttClientService;
    }

    public Linksoft.PowerController.Controller.RaspberryPi.Generated.Systems.Models.ControllerSystemInfo GetSystemInfo()
    {
        var serviceUptime = DateTimeOffset.UtcNow - serviceStartTime;
        var serverUptime = GetServerUptime();
        var devices = deviceRepository
            .GetAllAsync()
            .GetAwaiter()
            .GetResult();

        return TypeMapper.ToApiControllerSystemInfo(
            serviceUptime: serviceUptime.ToString(),
            serverUptime: serverUptime.ToString(),
            hostname: Environment.MachineName,
            operatingSystem: RuntimeInformation.OSDescription,
            shutdownState: shutdownOrchestrator.CurrentProgress.State,
            deviceCount: devices.Count,
            mqttConnected: mqttClientService?.IsConnected ?? false);
    }

    private static TimeSpan GetServerUptime()
    {
        if (OperatingSystem.IsLinux())
        {
            try
            {
                var uptimeText = File.ReadAllText("/proc/uptime");
                var uptimeSeconds = double.Parse(
                    uptimeText.Split(' ')[0],
                    CultureInfo.InvariantCulture);
                return TimeSpan.FromSeconds(uptimeSeconds);
            }
            catch
            {
                return TimeSpan.FromMilliseconds(Environment.TickCount64);
            }
        }

        return TimeSpan.FromMilliseconds(Environment.TickCount64);
    }
}