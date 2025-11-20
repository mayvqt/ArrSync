using System.Net;
using System.Text.Json;
using ArrSync.App.Models;
using ArrSync.App.Services.Http;
using Microsoft.Extensions.Options;
using Prometheus;

namespace ArrSync.App.Services.Clients;

public class OverseerClient : IOverseerClient
{
    private static readonly Counter OverseerCallCounter = Metrics.CreateCounter("arrsync_overseer_calls_total",
        "Total number of Overseer calls", new CounterConfiguration
        {
            LabelNames = new[] { "operation", "status" }
        });

    private static readonly Counter OverseerFailureCounter = Metrics.CreateCounter("arrsync_overseer_failures_total",
        "Total Overseer failures", new CounterConfiguration
        {
            LabelNames = new[] { "operation" }
        });

    private static readonly Histogram OverseerLatency = Metrics.CreateHistogram("arrsync_overseer_latency_seconds",
        "Overseer call latency in seconds", new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.01, 2, 10),
            LabelNames = new[] { "operation" }
        });

    private static readonly Gauge OverseerAvailableGauge = Metrics.CreateGauge("arrsync_overseer_available",
        "Overseer availability (1 = available, 0 = unavailable)");

    private readonly IOverseerHttp _http;
    private readonly ILogger<OverseerClient> _log;
    private readonly Config _opts;
    private bool _available = true;

    public OverseerClient(IOverseerHttp http, IOptions<Config> opts, ILogger<OverseerClient> log)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _opts = opts?.Value ?? throw new ArgumentNullException(nameof(opts));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        OverseerAvailableGauge.Set(_available ? 1 : 0);
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(_available);
    }

    public async Task<(bool ok, string details)> HealthCheckAsync(CancellationToken ct)
    {
        const string operation = Constants.Operations.Health;
        OverseerCallCounter.WithLabels(operation, Constants.MetricStatus.Start).Inc();
        using (OverseerLatency.WithLabels(operation).NewTimer())
        {
            try
            {
                using var resp = await _http.GetAsync("/api/v1/status", ct).ConfigureAwait(false);
                if (resp.IsSuccessStatusCode)
                {
                    _available = true;
                    OverseerAvailableGauge.Set(1);
                    OverseerCallCounter.WithLabels(operation, Constants.MetricStatus.Ok).Inc();
                    return (true, "ok");
                }

                _available = false;
                OverseerAvailableGauge.Set(0);
                OverseerCallCounter.WithLabels(operation, Constants.MetricStatus.Error).Inc();
                OverseerFailureCounter.WithLabels(operation).Inc();
                return (false, $"status: {(int)resp.StatusCode}");
            }
            catch (Exception ex)
            {
                _available = false;
                OverseerAvailableGauge.Set(0);
                OverseerCallCounter.WithLabels(operation, Constants.MetricStatus.Exception).Inc();
                OverseerFailureCounter.WithLabels(operation).Inc();
                _log.LogWarning(ex, "Overseer health check failed");
                return (false, ex.Message);
            }
        }
    }

    public async Task<int?> GetMediaIdByTmdbAsync(int tmdbId, string mediaType, CancellationToken ct)
    {
        const string operation = Constants.Operations.GetMedia;
        OverseerCallCounter.WithLabels(operation, Constants.MetricStatus.Start).Inc();
        if (!_available)
        {
            _log.LogWarning("Overseer unavailable, skipping lookup for tmdb {tmdbId}", tmdbId);
            OverseerCallCounter.WithLabels(operation, Constants.MetricStatus.Skipped).Inc();
            return null;
        }

        var url = $"/api/v1/{mediaType}/{tmdbId}"; // matches Go implementation

        var attempts = 0;
        var max = Math.Max(1, _opts.MaxRetries);

        Exception? lastEx = null;
        while (attempts <= max)
        {
            attempts++;
            using (OverseerLatency.WithLabels(operation).NewTimer())
            {
                try
                {
                    using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);

                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        OverseerCallCounter.WithLabels(operation, Constants.MetricStatus.NotFound).Inc();
                        return null; // not found
                    }

                    if (resp.IsSuccessStatusCode)
                    {
                        OverseerCallCounter.WithLabels(operation, Constants.MetricStatus.Ok).Inc();
                        var mediaId = await ParseMediaIdAsync(resp.Content, ct).ConfigureAwait(false);
                        if (mediaId.HasValue)
                        {
                            return mediaId.Value;
                        }

                        throw new InvalidOperationException("Could not find media id in response");
                    }

                    var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    throw new HttpRequestException($"Unexpected status {(int)resp.StatusCode}: {text}");
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    lastEx = ex;
                    OverseerFailureCounter.WithLabels(operation).Inc();
                    if (attempts <= max)
                    {
                        var backoff = ComputeBackoff(attempts);
                        _log.LogWarning(ex, "GetMediaIdByTmdb attempt {attempt} failed, backing off {backoff}",
                            attempts, backoff);
                        await Task.Delay(backoff, ct).ConfigureAwait(false);
                        continue;
                    }

                    _available = false;
                    _log.LogError(ex, "GetMediaIdByTmdb failed after {attempts} attempts", attempts);
                    throw;
                }
            }
        }

        if (lastEx != null)
        {
            throw lastEx;
        }

        return null;
    }

    public async Task<bool> DeleteMediaAsync(int id, CancellationToken ct)
    {
        const string operation = Constants.Operations.DeleteMedia;
        OverseerCallCounter.WithLabels(operation, Constants.MetricStatus.Start).Inc();
        if (!_available)
        {
            _log.LogWarning("Overseer unavailable, skipping delete id {id}", id);
            OverseerCallCounter.WithLabels(operation, Constants.MetricStatus.Skipped).Inc();
            return false;
        }

        var attempts = 0;
        var max = Math.Max(1, _opts.MaxRetries);

        while (attempts <= max)
        {
            attempts++;
            using (OverseerLatency.WithLabels(operation).NewTimer())
            {
                try
                {
                    using var resp = await _http.DeleteAsync($"/api/v1/media/{id}", ct).ConfigureAwait(false);
                    if (resp.IsSuccessStatusCode)
                    {
                        _available = true;
                        OverseerCallCounter.WithLabels(operation, Constants.MetricStatus.Ok).Inc();
                        return true;
                    }

                    var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    throw new HttpRequestException($"Unexpected status {(int)resp.StatusCode}: {text}");
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    OverseerFailureCounter.WithLabels(operation).Inc();
                    if (attempts <= max)
                    {
                        var backoff = ComputeBackoff(attempts);
                        _log.LogWarning(ex, "DeleteMedia attempt {attempt} failed, backing off {backoff}", attempts,
                            backoff);
                        await Task.Delay(backoff, ct).ConfigureAwait(false);
                        continue;
                    }

                    _available = false;
                    _log.LogError(ex, "DeleteMedia failed after {attempts} attempts", attempts);
                    throw;
                }
            }
        }

        return false;
    }

    private static async Task<int?> ParseMediaIdAsync(HttpContent content, CancellationToken ct)
    {
        await using var stream = await content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

        if (doc.RootElement.ValueKind == JsonValueKind.Object)
        {
            if (doc.RootElement.TryGetProperty("mediaInfo", out var mi) &&
                mi.ValueKind == JsonValueKind.Object && mi.TryGetProperty("id", out var idEl) &&
                idEl.ValueKind == JsonValueKind.Number)
            {
                var id = idEl.GetInt32();
                if (id != 0)
                {
                    return id;
                }
            }

            if (doc.RootElement.TryGetProperty("id", out var id2) && id2.ValueKind == JsonValueKind.Number)
            {
                var idVal = id2.GetInt32();
                if (idVal != 0)
                {
                    return idVal;
                }
            }
        }

        return null;
    }

    private TimeSpan ComputeBackoff(int attempt)
    {
        var maxBackoff = TimeSpan.FromSeconds(30);

        var exp = Math.Pow(2, Math.Max(0, attempt - 1));
        var baseSeconds = Math.Min(maxBackoff.TotalSeconds, Math.Max(0.1, _opts.InitialBackoffSeconds) * exp);
        var baseBackoff = TimeSpan.FromSeconds(baseSeconds);

        var jitter = Random.Shared.NextDouble() * 0.5 + 0.5;
        var backoff = TimeSpan.FromMilliseconds(baseBackoff.TotalMilliseconds * jitter);
        _log.LogDebug("Computed backoff for attempt {Attempt}: {BackoffMs}ms", attempt, backoff.TotalMilliseconds);
        return backoff;
    }
}
