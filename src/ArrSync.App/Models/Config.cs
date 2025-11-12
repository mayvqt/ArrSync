namespace ArrSync.App.Models;

public sealed class Config
{
    // Overseerr related
    public string? Url { get; set; }
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 10;
    public int MaxRetries { get; set; } = 3;
    public int InitialBackoffSeconds { get; set; } = 1;
    public bool DryRun { get; set; } = false;
    public int MonitorIntervalSeconds { get; set; } = 60;

    // Optional secret that webhook providers must provide in the X-Webhook-Secret header.
    // When set, incoming webhook endpoints will require this header value to match.
    public string? WebhookSecret { get; set; }

    // Future non-Overseer config fields can be added here
}