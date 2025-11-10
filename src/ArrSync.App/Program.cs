using ArrSync.App.Models;
using ArrSync.App.Services;
using Polly;
using Polly.Extensions.Http;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Bind application config from configuration section `ArrSync:Config` and environment vars
builder.Services.Configure<Config>(builder.Configuration.GetSection("ArrSync:Config"));
builder.Configuration.AddEnvironmentVariables();

var config = builder.Configuration;
// Try environment first, then ArrSync:Overseer section, then defaults
var overseerUrl = config["OVERSEER_URL"] ?? config["ArrSync:Config:Url"] ?? "http://localhost:5055";
var overseerKey = config["OVERSEER_API_KEY"] ?? config["ArrSync:Config:ApiKey"];
var timeoutSeconds = int.TryParse(config["TIMEOUT_SECONDS"], out var t) ? t : 10;
var logLevel = config["LOG_LEVEL"] ?? config["Logging:LogLevel:Default"] ?? "Information";

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

builder.Services.AddHttpClient<IOverseerClient, OverseerClient>("overseer", client =>
    {
        client.BaseAddress = new Uri(overseerUrl);
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
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
    .AddHttpMessageHandler(() => new PolicyHandler(Policy.WrapAsync(retryPolicy, circuitBreaker)));

// DI services
builder.Services.AddSingleton<CleanupService>();
builder.Services.AddHostedService<OverseerMonitorService>();

var app = builder.Build();

// metrics: expose /metrics and collect HTTP metrics
app.UseMetricServer();
app.UseHttpMetrics();

app.MapGet("/health", async (IOverseerClient oc, CancellationToken ct) =>
{
    var (ok, detail) = await oc.HealthCheckAsync(ct);
    return Results.Json(new { healthy = ok, details = detail });
});

app.MapPost("/webhook/sonarr", async (SonarrWebhook payload, CleanupService svc, CancellationToken ct) =>
{
    if (string.Equals(payload.EventType, "Test", StringComparison.OrdinalIgnoreCase))
        return Results.Ok(new { message = "test event ignored" });

    var tmdb = payload.Series?.TmdbId ?? 0;
    if (tmdb <= 0) return Results.Ok(new { message = "no tmdb id found" });

    await svc.ProcessSonarrAsync(tmdb, ct);
    return Results.Ok(new { message = "processed" });
});

app.MapPost("/webhook/radarr", async (RadarrWebhook payload, CleanupService svc, CancellationToken ct) =>
{
    if (string.Equals(payload.EventType, "Test", StringComparison.OrdinalIgnoreCase))
        return Results.Ok(new { message = "test event ignored" });

    var tmdb = payload.Movie?.TmdbId ?? 0;
    if (tmdb <= 0) return Results.Ok(new { message = "no tmdb id found" });

    await svc.ProcessRadarrAsync(tmdb, ct);
    return Results.Ok(new { message = "processed" });
});

app.Run();