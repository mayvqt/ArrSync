using ArrSync.App.Models;
using Microsoft.Extensions.Options;

namespace ArrSync.App.Services;

/// <summary>
/// Background service that periodically monitors Overseerr availability.
/// </summary>
public sealed class OverseerMonitorService : BackgroundService
{
    private readonly IOverseerClient _client;
    private readonly Config _opts;
    private readonly ILogger<OverseerMonitorService> _log;

    public OverseerMonitorService(
        IOverseerClient client, 
        IOptions<Config> opts,
        ILogger<OverseerMonitorService> log)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _opts = opts?.Value ?? throw new ArgumentNullException(nameof(opts));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _opts.MonitorIntervalSeconds));
        var failureCount = 0;

        // Initial delay to allow the app to start up
        await Task.Delay(interval, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (ok, details) = await _client.HealthCheckAsync(stoppingToken);
                if (ok)
                {
                    failureCount = 0;
                }
                else
                {
                    failureCount++;
                    _log.LogWarning("Overseerr health check failed: {Details}", details);
                }
            }
            catch (Exception ex)
            {
                failureCount++;
                _log.LogError(ex, "Exception during Overseerr health check");
            }

            // Adaptive backoff: increase delay on failures, up to 5x the configured interval
            var backoffMultiplier = Math.Min(1 + failureCount, 5);
            var nextDelay = TimeSpan.FromMilliseconds(interval.TotalMilliseconds * backoffMultiplier);
            await Task.Delay(nextDelay, stoppingToken);
        }
    }
}
