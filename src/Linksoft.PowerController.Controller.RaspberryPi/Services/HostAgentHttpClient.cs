namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// HTTP client for communicating with HostAgent REST API.
/// </summary>
public sealed class HostAgentHttpClient : IHostAgentHttpClient
{
    private readonly HttpClient httpClient;
    private readonly ILogger<HostAgentHttpClient> logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public HostAgentHttpClient(
        HttpClient httpClient,
        ILogger<HostAgentHttpClient> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;

        this.httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<HostAgentSystemInfo?> GetSystemInfoAsync(
        Domain.DeviceEntity device,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        var url = $"http://{device.IpAddress}:{device.Port}/api/v1/system/info";

        try
        {
            logger.LogDebug("Fetching system info from {Url}", url);

            var response = await httpClient
                .GetAsync(url, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var json = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            return JsonSerializer.Deserialize<HostAgentSystemInfo>(json, JsonOptions);
        }
        catch (HttpRequestException ex)
        {
            logger.LogDebug(
                ex,
                "Failed to fetch system info from {DeviceName} at {Url}",
                device.Name,
                url);
            return null;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            logger.LogDebug("Timeout fetching system info from {DeviceName}", device.Name);
            return null;
        }
    }

    public async Task<HostAgentShutdownResponse?> SendShutdownAsync(
        Domain.DeviceEntity device,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        var url = $"http://{device.IpAddress}:{device.Port}/api/v1/system/shutdown";

        try
        {
            logger.LogInformation(
                "Sending shutdown request to {DeviceName} at {Url}",
                device.Name,
                url);

            var content = new StringContent(
                JsonSerializer.Serialize(new { mode = "Immediate" }, JsonOptions),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await httpClient
                .PostAsync(url, content, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var json = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            return JsonSerializer.Deserialize<HostAgentShutdownResponse>(json, JsonOptions);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(
                ex,
                "Failed to send shutdown request to {DeviceName} at {Url}",
                device.Name,
                url);
            return null;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            logger.LogWarning("Timeout sending shutdown to {DeviceName}", device.Name);
            return null;
        }
    }
}