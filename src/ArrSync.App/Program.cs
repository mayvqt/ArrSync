using ArrSync.App;
using ArrSync.App.Configuration;
using ArrSync.App.Endpoints;
using ArrSync.App.Models;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Bind application config from configuration section `ArrSync:Config` and environment vars
builder.Services.Configure<Config>(builder.Configuration.GetSection("ArrSync:Config"));
builder.Configuration.AddEnvironmentVariables();

var config = builder.Configuration;
// Try environment first, then ArrSync:Overseer section, then defaults
// Support both OVERSEER_URL and ARR_SYNC style config for backward compatibility.
var overseerUrl = config["OVERSEER_URL"] ?? config["ArrSync:Config:OverseerUrl"] ?? config["ArrSync:Config:Url"] ?? "http://localhost:5055";
var overseerKey = config["OVERSEER_API_KEY"] ?? config["ArrSync:Config:ApiKey"];
// Number of seconds to wait for an Overseer HTTP request before timing out.
// Increased default from 10 to 30s to avoid premature TaskCanceledExceptions when Overseer is slow.
var timeoutSeconds = int.TryParse(config["TIMEOUT_SECONDS"], out var t) ? t : 30;
// Webhook listen port: can be set via ArrSync:Config:Port or WEBHOOK_PORT env var. If set, we'll instruct the
// host to listen on that port (0.0.0.0 by default). This is separate from the Overseer Url which points to the
// external Overseerr service.
var webhookPortStr = config["WEBHOOK_PORT"] ?? config["ArrSync:Config:Port"];
var webhookPort = int.TryParse(webhookPortStr, out var p) ? p : (int?)null;
var logLevel = config["LOG_LEVEL"] ?? config["Logging:LogLevel:Default"] ?? "Information";

// If ArrSync config provided a webhook port, configure the host to listen on that port now (before Build).
if (webhookPort.HasValue)
{
    var listenUrl = $"http://0.0.0.0:{webhookPort.Value}";
    builder.WebHost.UseUrls(listenUrl);
}

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});
builder.Logging.SetMinimumLevel(Enum.TryParse<LogLevel>(logLevel, true, out var ll) ? ll : LogLevel.Information);

// Configure typed HttpClient for Overseer with Polly policies and tuned handler
var maxRetries = int.TryParse(config["ArrSync:Config:MaxRetries"], out var mr) ? mr : 3;
var initialBackoff = int.TryParse(config["ArrSync:Config:InitialBackoffSeconds"], out var ib) ? ib : 1;

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(Enumerable.Range(1, Math.Max(1, maxRetries)).Select(i =>
        TimeSpan.FromSeconds(Math.Min(30, initialBackoff * Math.Pow(2, i)))).ToArray());

var circuitBreaker = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

// Per-attempt timeout controlled by Polly. We set the HttpClient.Timeout to infinite
// and rely on the Polly timeout so the timeout is applied per attempt and cooperates
// with Polly's retry/circuit-breaker behavior.
var timeoutPolicy =
    Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(timeoutSeconds), TimeoutStrategy.Optimistic);

// Wrap policies so the timeout applies to each try.
var combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreaker, timeoutPolicy);

builder.Services.AddHttpClient<IOverseerClient, OverseerClient>("overseer", client =>
    {
        client.BaseAddress = new Uri(overseerUrl);
        // Use infinite on HttpClient so Polly's TimeoutPolicy controls per-attempt timing.
        client.Timeout = Timeout.InfiniteTimeSpan;
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        if (!string.IsNullOrWhiteSpace(overseerKey)) client.DefaultRequestHeaders.Add("X-Api-Key", overseerKey);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        PooledConnectionIdleTimeout = TimeSpan.FromSeconds(90),
        MaxConnectionsPerServer = 10,
        ConnectTimeout = TimeSpan.FromSeconds(timeoutSeconds)
    })
    .AddHttpMessageHandler(() => new PolicyHandler(combinedPolicy));

// DI services
builder.Services.AddSingleton<CleanupService>();
builder.Services.AddHostedService<OverseerMonitorService>();

var app = builder.Build();
// Log resolved ArrSync configuration at startup (mask secrets)
try
{
    var resolvedConfig = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Config>>().Value;
    var maskedKey = string.IsNullOrWhiteSpace(resolvedConfig.ApiKey) ? "<none>" : "****REDACTED****";
    app.Logger.LogInformation("ArrSync configuration: OverseerUrl={url} DryRun={dryRun} MonitorInterval={mi} WebhookSecretConfigured={hasSecret}",
        resolvedConfig.OverseerUrl ?? "<unspecified>", resolvedConfig.DryRun, resolvedConfig.MonitorIntervalSeconds, !string.IsNullOrWhiteSpace(resolvedConfig.WebhookSecret));
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Failed to read ArrSync configuration on startup");
}

// metrics: expose /metrics and collect HTTP metrics
app.UseMetricServer();
app.UseHttpMetrics();

app.MapGet("/health", async (IOverseerClient oc, CancellationToken ct) =>
{
    var (ok, detail) = await oc.HealthCheckAsync(ct);
    var healthy = ok;
    var status = healthy ? "healthy" : "degraded";
    var overseerStatus = await oc.IsAvailableAsync() ? "available" : "unavailable";
    var payload = new { status, service = "arrsync", healthy, overseer = overseerStatus };
    return Results.Json(payload, statusCode: healthy ? 200 : 503);
});

app.MapPost("/webhook/sonarr", async (HttpRequest req, SonarrWebhook payload, CleanupService svc, CancellationToken ct) =>
{
    // Optional webhook secret enforcement
    var cfg = req.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<Config>>().Value;
    if (!string.IsNullOrWhiteSpace(cfg.WebhookSecret))
    {
        if (!req.Headers.TryGetValue("X-Webhook-Secret", out var v) || v != cfg.WebhookSecret)
            return Results.Unauthorized();
    }

    if (string.Equals(payload.EventType, "Test", StringComparison.OrdinalIgnoreCase))
        return Results.Ok(new { message = "test event ignored" });

    var tmdb = payload.Series?.TmdbId ?? 0;
    if (tmdb <= 0) return Results.Ok(new { message = "no tmdb id found" });

    await svc.ProcessSonarrAsync(tmdb, ct);
    return Results.Ok(new { message = "processed" });
});

app.MapPost("/webhook/radarr", async (HttpRequest req, RadarrWebhook payload, CleanupService svc, CancellationToken ct) =>
{
    var cfg = req.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<Config>>().Value;
    if (!string.IsNullOrWhiteSpace(cfg.WebhookSecret))
    {
        if (!req.Headers.TryGetValue("X-Webhook-Secret", out var v) || v != cfg.WebhookSecret)
            return Results.Unauthorized();
    }

    if (string.Equals(payload.EventType, "Test", StringComparison.OrdinalIgnoreCase))
        return Results.Ok(new { message = "test event ignored" });

    var tmdb = payload.Movie?.TmdbId ?? 0;
    if (tmdb <= 0) return Results.Ok(new { message = "no tmdb id found" });

    await svc.ProcessRadarrAsync(tmdb, ct);
    return Results.Ok(new { message = "processed" });
});

app.Run();