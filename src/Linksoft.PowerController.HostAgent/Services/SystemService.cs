// ReSharper disable InvertIf
namespace Linksoft.PowerController.HostAgent.Services;

public sealed class SystemService : ISystemService, IDisposable, IAsyncDisposable
{
    private readonly DateTimeOffset serviceStartTime = DateTimeOffset.UtcNow;
    private readonly SemaphoreSlim shutdownLock = new(1, 1);
    private CancellationTokenSource? shutdownCts;

    public bool IsShutdownInProgress { get; private set; }

    public DateTimeOffset? ShutdownScheduledAt { get; private set; }

    public SystemInfo GetSystemInfo()
    {
        var serviceUptime = DateTimeOffset.UtcNow - serviceStartTime;
        var serverUptime = GetServerUptime();

        return new SystemInfo(
            ServiceUptime: serviceUptime.ToString(),
            ServerUptime: serverUptime.ToString(),
            ShutdownInProgress: IsShutdownInProgress,
            ShutdownScheduledAt: ShutdownScheduledAt,
            OperatingSystem: RuntimeInformation.OSDescription,
            Hostname: Environment.MachineName);
    }

    public async Task<ShutdownResponse> InitiateShutdownAsync(
        ShutdownMode mode,
        int? delaySeconds,
        DateTimeOffset? scheduledAt,
        CancellationToken cancellationToken = default)
    {
        await shutdownLock
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            if (IsShutdownInProgress)
            {
                throw new InvalidOperationException("Shutdown already in progress");
            }

            var shutdownTime = CalculateShutdownTime(mode, delaySeconds, scheduledAt);
            var delay = shutdownTime - DateTimeOffset.UtcNow;

            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            IsShutdownInProgress = true;
            ShutdownScheduledAt = shutdownTime;

            shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ = ExecuteShutdownAsync(delay, shutdownCts.Token);

            return new ShutdownResponse(
                Message: $"Shutdown scheduled for {shutdownTime:O}",
                ShutdownScheduledAt: shutdownTime);
        }
        finally
        {
            shutdownLock.Release();
        }
    }

    public async Task CancelShutdownAsync(
        CancellationToken cancellationToken = default)
    {
        await shutdownLock
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            if (!IsShutdownInProgress)
            {
                return;
            }

            if (shutdownCts is not null)
            {
                await shutdownCts
                    .CancelAsync()
                    .ConfigureAwait(false);
            }

            IsShutdownInProgress = false;
            ShutdownScheduledAt = null;
        }
        finally
        {
            shutdownLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (shutdownCts is not null)
        {
            await shutdownCts
                .CancelAsync()
                .ConfigureAwait(false);
            shutdownCts.Dispose();
        }

        shutdownLock.Dispose();
    }

    public void Dispose()
    {
        DisposeAsync()
            .AsTask()
            .GetAwaiter()
            .GetResult();
    }

    private static DateTimeOffset CalculateShutdownTime(
        ShutdownMode mode,
        int? delaySeconds,
        DateTimeOffset? scheduledAt)
        => mode switch
        {
            ShutdownMode.Immediate => DateTimeOffset.UtcNow,
            ShutdownMode.Delayed => DateTimeOffset.UtcNow.AddSeconds(delaySeconds ?? 60),
            ShutdownMode.Scheduled => scheduledAt ?? DateTimeOffset.UtcNow,
            _ => DateTimeOffset.UtcNow,
        };

    private static TimeSpan GetServerUptime()
    {
        if (OperatingSystem.IsWindows())
        {
            return TimeSpan.FromMilliseconds(Environment.TickCount64);
        }

        if (OperatingSystem.IsLinux())
        {
            try
            {
                var uptimeText = File.ReadAllText("/proc/uptime");
                var uptimeSeconds = double.Parse(uptimeText.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
                return TimeSpan.FromSeconds(uptimeSeconds);
            }
            catch
            {
                return TimeSpan.FromMilliseconds(Environment.TickCount64);
            }
        }

        return TimeSpan.FromMilliseconds(Environment.TickCount64);
    }

    private async Task ExecuteShutdownAsync(
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        try
        {
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            ExecuteSystemShutdown();
        }
        catch (OperationCanceledException)
        {
            // Shutdown was cancelled
        }
        finally
        {
            await shutdownLock
                .WaitAsync(CancellationToken.None)
                .ConfigureAwait(false);

            try
            {
                IsShutdownInProgress = false;
                ShutdownScheduledAt = null;
            }
            finally
            {
                shutdownLock.Release();
            }
        }
    }

    private static void ExecuteSystemShutdown()
    {
        ProcessStartInfo startInfo;

        if (OperatingSystem.IsWindows())
        {
            startInfo = new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = "/s /t 0",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
        }
        else if (OperatingSystem.IsLinux())
        {
            startInfo = new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = "now",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
        }
        else
        {
            throw new PlatformNotSupportedException("Shutdown is not supported on this platform");
        }

        using var process = Process.Start(startInfo);
    }
}