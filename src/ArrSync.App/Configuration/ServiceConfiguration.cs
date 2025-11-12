using ArrSync.App.Models;
using ArrSync.App.Services;
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
        var overseerUrl = configuration[Constants.ConfigKeys.OverseerUrl] ?? "http://localhost:5055";
        var overseerKey = configuration[Constants.ConfigKeys.OverseerApiKey];
        var timeoutSeconds = int.TryParse(configuration[Constants.ConfigKeys.TimeoutSeconds], out var t) ? t : 30;
        var maxRetries = int.TryParse(configuration[Constants.ConfigKeys.MaxRetries], out var mr) ? mr : 3;
        var initialBackoff = int.TryParse(configuration[Constants.ConfigKeys.InitialBackoffSeconds], out var ib) ? ib : 1;

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

        services.AddHttpClient<IOverseerClient, OverseerClient>("overseer", client =>
            {
                client.BaseAddress = new Uri(overseerUrl);
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

        return services;
    }

    /// <summary>
    /// Configures application services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<CleanupService>();
        services.AddHostedService<OverseerMonitorService>();
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
