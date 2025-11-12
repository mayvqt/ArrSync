using System.Net;
using System.Text.Json;
using ArrSync.App.Models;
using Microsoft.Extensions.Options;
using Prometheus;

namespace ArrSync.App.Services;

public class OverseerClient : IOverseerClient
{
    // Prometheus metrics
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

    private readonly HttpClient _client;
    private readonly ILogger<OverseerClient> _log;
    private readonly Config _opts;
    private readonly Random _rng = new();
    private bool _available = true;

    public OverseerClient(HttpClient client, IOptions<Config> opts, ILogger<OverseerClient> log)
    {
        _client = client;
        _opts = opts.Value;
        _log = log;
        OverseerAvailableGauge.Set(_available ? 1 : 0);
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(_available);
    }

    public async Task<(bool ok, string details)> HealthCheckAsync(CancellationToken ct)
    {
        var operation = "health";
        OverseerCallCounter.WithLabels(operation, "start").Inc();
        using (OverseerLatency.WithLabels(operation).NewTimer())
        {
            try
            {
                using var resp = await _client.GetAsync("/api/v1/status", ct);
                if (resp.IsSuccessStatusCode)
                {
                    _available = true;
                    OverseerAvailableGauge.Set(1);
                    OverseerCallCounter.WithLabels(operation, "ok").Inc();
                    return (true, "ok");
                }

                _available = false;
                OverseerAvailableGauge.Set(0);
                OverseerCallCounter.WithLabels(operation, "error").Inc();
                OverseerFailureCounter.WithLabels(operation).Inc();
                return (false, $"status: {(int)resp.StatusCode}");
            }
            catch (Exception ex)
            {
                _available = false;
                OverseerAvailableGauge.Set(0);
                OverseerCallCounter.WithLabels(operation, "exception").Inc();
                OverseerFailureCounter.WithLabels(operation).Inc();
                _log.LogWarning(ex, "Overseer health check failed");
                return (false, ex.Message);
            }
        }
    }

    public async Task<int?> GetMediaIdByTmdbAsync(int tmdbId, string mediaType, CancellationToken ct)
    {
        var operation = "getMedia";
        OverseerCallCounter.WithLabels(operation, "start").Inc();
        if (!_available)
        {
            _log.LogWarning("Overseer unavailable, skipping lookup for tmdb {tmdbId}", tmdbId);
            OverseerCallCounter.WithLabels(operation, "skipped").Inc();
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
                    using var resp = await _client.GetAsync(url, ct);

                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        OverseerCallCounter.WithLabels(operation, "notfound").Inc();
                        return null; // not found
                    }

                    if (resp.IsSuccessStatusCode)
                    {
                        OverseerCallCounter.WithLabels(operation, "ok").Inc();
                        using var stream = await resp.Content.ReadAsStreamAsync(ct);
                        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                        if (doc.RootElement.TryGetProperty("mediaInfo", out var mi) &&
                            mi.ValueKind == JsonValueKind.Object && mi.TryGetProperty("id", out var idEl) &&
                            idEl.GetInt32() != 0) return idEl.GetInt32();
                        if (doc.RootElement.TryGetProperty("id", out var id2) && id2.ValueKind == JsonValueKind.Number)
                        {
                            var idVal = id2.GetInt32();
                            if (idVal != 0) return idVal;
                        }

                        throw new InvalidOperationException("Could not find media id in response");
                    }

                    var text = await resp.Content.ReadAsStringAsync(ct);
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
                        await Task.Delay(backoff, ct);
                        continue;
                    }

                    _available = false;
                    _log.LogError(ex, "GetMediaIdByTmdb failed after {attempts} attempts", attempts);
                    throw;
                }
            }
        }

        if (lastEx != null) throw lastEx;
        return null;
    }

    public async Task<bool> DeleteMediaAsync(int id, CancellationToken ct)
    {
        var operation = "deleteMedia";
        OverseerCallCounter.WithLabels(operation, "start").Inc();
        if (!_available)
        {
            _log.LogWarning("Overseer unavailable, skipping delete id {id}", id);
            OverseerCallCounter.WithLabels(operation, "skipped").Inc();
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
                    using var resp = await _client.DeleteAsync($"/api/v1/media/{id}", ct);
                    if (resp.IsSuccessStatusCode)
                    {
                        _available = true;
                        OverseerCallCounter.WithLabels(operation, "ok").Inc();
                        return true;
                    }

                    var text = await resp.Content.ReadAsStringAsync(ct);
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
                        await Task.Delay(backoff, ct);
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

    private TimeSpan ComputeBackoff(int attempt)
    {
        var maxBackoff = TimeSpan.FromSeconds(30);
        var baseBackoff =
            TimeSpan.FromSeconds(Math.Min(maxBackoff.TotalSeconds, _opts.InitialBackoffSeconds * Math.Pow(2, attempt)));
        // full jitter up to baseBackoff
        var jitter = _rng.NextDouble();
        return TimeSpan.FromMilliseconds(baseBackoff.TotalMilliseconds * jitter);
    }
}