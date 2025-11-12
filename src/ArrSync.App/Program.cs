using ArrSync.App;
using ArrSync.App.Configuration;
using ArrSync.App.Endpoints;
using ArrSync.App.Models;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Bind application config from configuration section `ArrSync:Config` and environment vars
builder.Services.Configure<Config>(builder.Configuration.GetSection("ArrSync:Config"));
builder.Configuration.AddEnvironmentVariables();

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
    app.Logger.LogInformation(
        "Starting ArrSync - OverseerUrl={OverseerUrl} DryRun={DryRun} MonitorInterval={MonitorInterval}s",
        config.OverseerUrl ?? "http://localhost:5055",
        config.DryRun,
        config.MonitorIntervalSeconds);
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