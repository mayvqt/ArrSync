using System;
using System.IO;
using ArrSync.App;
using ArrSync.App.Configuration;
using ArrSync.App.Endpoints;
using ArrSync.App.Models;
using Prometheus;

// Use the assembly folder as the content root so appsettings.* next to the binary are discovered
var contentRoot = AppContext.BaseDirectory;

// Prefer explicit environment variables; if none are set, pick Development when a dev config file exists
// next to the assembly, otherwise default to Production.
var envFromEnv = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
             ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var defaultEnv = File.Exists(Path.Combine(contentRoot, "appsettings.Development.json")) ? "Development" : "Production";
var environmentName = string.IsNullOrWhiteSpace(envFromEnv) ? defaultEnv : envFromEnv;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot,
    EnvironmentName = environmentName,
});

// Bind application config from configuration section `ArrSync:Config` and environment vars
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddOptions<Config>()
    .Bind(builder.Configuration.GetSection("ArrSync:Config"))
    .ValidateOnStart();

// Configure webhook port if specified
var webhookPort = int.TryParse(
    builder.Configuration[Constants.ConfigKeys.WebhookPort],
    out var port) ? port : (int?)null;

if (webhookPort.HasValue)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{webhookPort.Value}");
}

// Configure logging
builder.Logging.ConfigureAppLogging(builder.Configuration);

// Add Overseerr HTTP client with retry/circuit breaker policies
builder.Services.AddOverseerrClient(builder.Configuration);

// Add application services
builder.Services.AddApplicationServices();

var app = builder.Build();

// Log startup configuration
try
{
    var config = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Config>>().Value;
    // Sanity-check configuration and log a short, non-sensitive summary
    var overseerUrl = string.IsNullOrWhiteSpace(config.OverseerUrl) ? "http://localhost:5055" : config.OverseerUrl;
    if (string.IsNullOrWhiteSpace(config.OverseerUrl))
    {
        app.Logger.LogWarning("OverseerUrl not configured, falling back to default {DefaultUrl}", overseerUrl);
    }

    if (config.TimeoutSeconds < 1)
    {
        app.Logger.LogWarning("TimeoutSeconds value {Timeout} is invalid, using 30s", config.TimeoutSeconds);
        config.TimeoutSeconds = 30;
    }

    if (config.MonitorIntervalSeconds < 1)
    {
        app.Logger.LogWarning("MonitorIntervalSeconds value {Interval} is invalid, using 60s", config.MonitorIntervalSeconds);
        config.MonitorIntervalSeconds = 60;
    }

    app.Logger.LogInformation(
        "Starting ArrSync - OverseerUrl={OverseerUrl} DryRun={DryRun} MonitorInterval={MonitorInterval}s Timeout={Timeout}s",
        overseerUrl,
        config.DryRun,
        config.MonitorIntervalSeconds,
        config.TimeoutSeconds);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Failed to read configuration on startup");
}

// Configure Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

// Map endpoints
app.MapHealthEndpoint();
app.MapSonarrWebhookEndpoint();
app.MapRadarrWebhookEndpoint();

app.Run();