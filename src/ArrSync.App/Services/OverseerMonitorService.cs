using ArrSync.App.Models;
using Microsoft.Extensions.Options;

namespace ArrSync.App.Services;

public class OverseerMonitorService : BackgroundService
{
    private readonly IOverseerClient _client;
    private readonly Config _opts;

    public OverseerMonitorService(IOverseerClient client, IOptions<Config> opts)
    {
        _client = client;
        _opts = opts.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _opts.MonitorIntervalSeconds));
        // Wait the first interval before doing the initial check so the rest of the app can come up
        // and to avoid noisy logs if Overseer is momentarily unavailable at startup.
        var failureCount = 0;
        await Task.Delay(interval, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (ok, _) = await _client.HealthCheckAsync(stoppingToken);
                if (ok)
                {
                    failureCount = 0; // reset on success
                }
                else
                {
                    failureCount++;
                }
            }
            catch
            {
                // client already logs; increment failure count to back off
                failureCount++;
            }

            // adaptive delay: increase delay when failures happen, up to 5x the configured interval
            var backoffMultiplier = Math.Min(1 + failureCount, 5);
            var nextDelay = TimeSpan.FromMilliseconds(interval.TotalMilliseconds * backoffMultiplier);
            await Task.Delay(nextDelay, stoppingToken);
        }
    }
}