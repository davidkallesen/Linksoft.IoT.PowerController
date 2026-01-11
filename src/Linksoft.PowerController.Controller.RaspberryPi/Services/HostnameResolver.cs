namespace Linksoft.PowerController.Controller.RaspberryPi.Services;

/// <summary>
/// Service for resolving hostnames from IP addresses using Atc.Network.
/// </summary>
[Registration(Lifetime.Singleton, As = typeof(IHostnameResolver))]
public sealed class HostnameResolver : IHostnameResolver
{
    private readonly ILogger<HostnameResolver> logger;

    public HostnameResolver(ILogger<HostnameResolver> logger)
    {
        this.logger = logger;
    }

    public async Task<string?> ResolveHostnameAsync(
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);

        if (!IPAddress.TryParse(ipAddress, out var ip))
        {
            logger.LogWarning("Invalid IP address format: {IpAddress}", ipAddress);
            return null;
        }

        try
        {
            var hostname = await DnsLookupHelper
                .GetHostname(ip, cancellationToken)
                .ConfigureAwait(false);

            if (hostname is not null)
            {
                logger.LogDebug(
                    "Resolved hostname {Hostname} for IP {IpAddress}",
                    hostname,
                    ipAddress);
            }

            return hostname;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to resolve hostname for {IpAddress}", ipAddress);
            return null;
        }
    }
}