using ArrSync.App.Models;
using ArrSync.App.Services.Cleanup;
using ArrSync.App.Services.Monitoring;
using ArrSync.App.Services.Http;
using ArrSync.App.Services.Clients;
// Note: OverseerClient implementation lives in Services.Clients namespace. We reference it by fully-qualified name below to avoid ambiguity.
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace ArrSync.App.Configuration;

/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Configures the Overseerr HTTP client with retry and circuit breaker policies.
    /// </summary>
    public static IServiceCollection AddOverseerrClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Read values and apply sane defaults / validation
        var overseerUrl = configuration[Constants.ConfigKeys.OverseerUrl];
        if (string.IsNullOrWhiteSpace(overseerUrl))
        {
            overseerUrl = "http://localhost:5055"; // safe default
        }

        // Ensure we have a valid absolute URI for the HttpClient base address
        if (!Uri.TryCreate(overseerUrl, UriKind.Absolute, out var baseUri))
        {
            // Fallback to localhost if the configured value is malformed
            baseUri = new Uri("http://localhost:5055");
        }

        var overseerKey = configuration[Constants.ConfigKeys.OverseerApiKey];

        var timeoutSeconds = int.TryParse(configuration[Constants.ConfigKeys.TimeoutSeconds], out var t) ? t : 30;
        timeoutSeconds = Math.Max(1, timeoutSeconds);

        var maxRetries = int.TryParse(configuration[Constants.ConfigKeys.MaxRetries], out var mr) ? mr : 3;
        maxRetries = Math.Max(0, maxRetries);

        var initialBackoff = int.TryParse(configuration[Constants.ConfigKeys.InitialBackoffSeconds], out var ib) ? ib : 1;
        if (initialBackoff <= 0) initialBackoff = 1;

        // Retry policy with exponential backoff
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(Enumerable.Range(1, Math.Max(1, maxRetries)).Select(i =>
                TimeSpan.FromSeconds(Math.Min(30, initialBackoff * Math.Pow(2, i)))).ToArray());

        // Circuit breaker to prevent cascade failures
        var circuitBreaker = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

        // Per-request timeout using Polly's optimistic timeout strategy
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(timeoutSeconds),
            TimeoutStrategy.Optimistic);

        // Combine policies: timeout applies per retry attempt
        var combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreaker, timeoutPolicy);

    // Register the concrete typed client and expose it via the IOverseerClient interface
    // Register the low-level HTTP abstraction as a typed client so it receives the configured HttpClient
    services.AddHttpClient<ArrSync.App.Services.Http.OverseerHttp>(client =>
            {
                client.BaseAddress = baseUri;
                client.Timeout = Timeout.InfiniteTimeSpan; // Polly handles timeouts
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add(Constants.Headers.Accept, "application/json");
                if (!string.IsNullOrWhiteSpace(overseerKey))
                    client.DefaultRequestHeaders.Add(Constants.Headers.ApiKey, overseerKey);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(90),
                MaxConnectionsPerServer = 10,
                ConnectTimeout = TimeSpan.FromSeconds(timeoutSeconds)
            })
            .AddHttpMessageHandler(() => new PolicyHandler(combinedPolicy));

    // Expose the typed OverseerHttp via its testable interface so consumers can depend on IOverseerHttp
    services.AddTransient<ArrSync.App.Services.Http.IOverseerHttp>(sp => sp.GetRequiredService<ArrSync.App.Services.Http.OverseerHttp>());

    // Register the high-level client that depends on the IOverseerHttp abstraction
    services.AddTransient<ArrSync.App.Services.Clients.OverseerClient>();
    services.AddTransient<IOverseerClient>(sp => sp.GetRequiredService<ArrSync.App.Services.Clients.OverseerClient>());

        return services;
    }

    /// <summary>
    /// Configures application services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
    // Register cleanup service by its interface for testability and clearer DI boundaries
    services.AddSingleton<ICleanupService, CleanupService>();
        services.AddSingleton<ArrSync.App.Services.Timing.IPeriodicTimerFactory, ArrSync.App.Services.Timing.PeriodicTimerFactory>();
        services.AddHostedService<OverseerMonitorService>();

    // Register config validator so the app fails fast on invalid configuration
    services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<ArrSync.App.Models.Config>, ConfigValidation>();
        return services;
    }

    /// <summary>
    /// Configures logging with structured output.
    /// </summary>
    public static ILoggingBuilder ConfigureAppLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration)
    {
        var logLevel = configuration[Constants.ConfigKeys.LogLevel] ?? "Information";

        logging.ClearProviders();
        logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        });
        logging.SetMinimumLevel(Enum.TryParse<LogLevel>(logLevel, true, out var ll) ? ll : LogLevel.Information);

        return logging;
    }
}
