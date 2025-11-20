namespace ArrSync.App.Models;

public sealed class Config {
    public string? OverseerUrl { get; set; }
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public int InitialBackoffSeconds { get; set; } = 1;
    public bool DryRun { get; set; }
    public int MonitorIntervalSeconds { get; set; } = 60;

    public string? WebhookSecret { get; set; }
}
