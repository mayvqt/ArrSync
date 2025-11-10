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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _client.HealthCheckAsync(stoppingToken);
            }
            catch
            {
                // logging happens in client
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}