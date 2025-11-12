namespace ArrSync.App.Models;

/// <summary>
/// Application configuration for ArrSync.
/// </summary>
public sealed class Config
{
    /// <summary>
    /// Overseerr base URL (e.g. http://overseer:5055).
    /// </summary>
    public string? OverseerUrl { get; set; }
    
    /// <summary>
    /// API key for authenticating with Overseerr.
    /// </summary>
    public string? ApiKey { get; set; }
    
    /// <summary>
    /// HTTP request timeout in seconds. Default is 30.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Maximum number of retry attempts for failed requests. Default is 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Initial backoff delay in seconds for retry attempts. Default is 1.
    /// </summary>
    public int InitialBackoffSeconds { get; set; } = 1;
    
    /// <summary>
    /// When true, operations are logged but not executed. Default is false.
    /// </summary>
    public bool DryRun { get; set; }
    
    /// <summary>
    /// Interval in seconds between Overseerr health checks. Default is 60.
    /// </summary>
    public int MonitorIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Optional secret that webhook providers must provide in the X-Webhook-Secret header.
    /// When set, incoming webhook endpoints will require this header value to match.
    /// </summary>
    public string? WebhookSecret { get; set; }
}
