using ArrSync.App.Models;
using ArrSync.App.Services.Cleanup;
using ArrSync.App.Services.Clients;
using ArrSync.App.Services.Http;
using ArrSync.App.Services.Monitoring;
using ArrSync.App.Services.Timing;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace ArrSync.App.Configuration;

public static class ServiceConfiguration
{
    public static IServiceCollection AddOverseerrClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var overseerUrl = configuration[Constants.ConfigKeys.OverseerUrl];
        if (string.IsNullOrWhiteSpace(overseerUrl))
        {
            overseerUrl = "http://localhost:5055"; // safe default
        }

        if (!Uri.TryCreate(overseerUrl, UriKind.Absolute, out var baseUri))
        {
            baseUri = new Uri("http://localhost:5055");
        }

        var overseerKey = configuration[Constants.ConfigKeys.OverseerApiKey];

        var timeoutSeconds = int.TryParse(configuration[Constants.ConfigKeys.TimeoutSeconds], out var t) ? t : 30;
        timeoutSeconds = Math.Max(1, timeoutSeconds);

        var maxRetries = int.TryParse(configuration[Constants.ConfigKeys.MaxRetries], out var mr) ? mr : 3;
        maxRetries = Math.Max(0, maxRetries);

        var initialBackoff = int.TryParse(configuration[Constants.ConfigKeys.InitialBackoffSeconds], out var ib)
            ? ib
            : 1;
        if (initialBackoff <= 0)
        {
            initialBackoff = 1;
        }

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(Enumerable.Range(1, Math.Max(1, maxRetries)).Select(i =>
                TimeSpan.FromSeconds(Math.Min(30, initialBackoff * Math.Pow(2, i)))).ToArray());
        var circuitBreaker = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(timeoutSeconds),
            TimeoutStrategy.Optimistic);
        var combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreaker, timeoutPolicy);
        services.AddHttpClient<OverseerHttp>(client =>
            {
                client.BaseAddress = baseUri;
                client.Timeout = Timeout.InfiniteTimeSpan; // Polly handles timeouts
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add(Constants.Headers.Accept, "application/json");
                if (!string.IsNullOrWhiteSpace(overseerKey))
                {
                    client.DefaultRequestHeaders.Add(Constants.Headers.ApiKey, overseerKey);
                }
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(90),
                MaxConnectionsPerServer = 10,
                ConnectTimeout = TimeSpan.FromSeconds(timeoutSeconds)
            })
            .AddHttpMessageHandler(() => new PolicyHandler(combinedPolicy));
        services.AddTransient<IOverseerHttp>(sp => sp.GetRequiredService<OverseerHttp>());
        services.AddTransient<OverseerClient>();
        services.AddTransient<IOverseerClient>(sp => sp.GetRequiredService<OverseerClient>());

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<ICleanupService, CleanupService>();
        services.AddSingleton<IPeriodicTimerFactory, PeriodicTimerFactory>();
        services.AddHostedService<OverseerMonitorService>();
        services.AddSingleton<IValidateOptions<Config>, ConfigValidation>();
        return services;
    }

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
