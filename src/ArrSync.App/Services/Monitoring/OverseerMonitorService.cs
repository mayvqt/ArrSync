using ArrSync.App.Models;
using ArrSync.App.Services.Clients;
using ArrSync.App.Services.Timing;
using Microsoft.Extensions.Options;

namespace ArrSync.App.Services.Monitoring;

public sealed class OverseerMonitorService : BackgroundService
{
    private readonly IOverseerClient _client;
    private readonly ILogger<OverseerMonitorService> _log;
    private readonly Config _opts;
    private readonly IPeriodicTimerFactory _timerFactory;

    public OverseerMonitorService(
        IOverseerClient client,
        IOptions<Config> opts,
        ILogger<OverseerMonitorService> log,
        IPeriodicTimerFactory timerFactory)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _opts = opts?.Value ?? throw new ArgumentNullException(nameof(opts));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _timerFactory = timerFactory ?? throw new ArgumentNullException(nameof(timerFactory));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var baseInterval = TimeSpan.FromSeconds(Math.Max(1, _opts.MonitorIntervalSeconds));
        var failureCount = 0;


        try
        {
            await Task.Delay(baseInterval, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (ok, details) = await _client.HealthCheckAsync(stoppingToken).ConfigureAwait(false);
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


            var nextDelay = ComputeNextDelay(baseInterval, failureCount);


            using var timer = _timerFactory.Create(nextDelay);
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    public static TimeSpan ComputeNextDelay(TimeSpan baseInterval, int failureCount)
    {
        var backoffMultiplier = Math.Min(1 + failureCount, 5);
        return TimeSpan.FromMilliseconds(baseInterval.TotalMilliseconds * backoffMultiplier);
    }

    internal Task RunAsync(CancellationToken ct)
    {
        return ExecuteAsync(ct);
    }
}
