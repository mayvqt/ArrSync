namespace ArrSync.App;

public static class Constants
{
    public static class MediaTypes
    {
        public const string Movie = "movie";
        public const string Tv = "tv";
    }

    public static class EventTypes
    {
        public const string Test = "Test";
    }

    public static class Headers
    {
        public const string WebhookSecret = "X-Webhook-Secret";
        public const string ApiKey = "X-Api-Key";
        public const string Accept = "Accept";
    }

    public static class ConfigKeys
    {
        public const string OverseerUrl = "OVERSEER_URL";
        public const string OverseerApiKey = "OVERSEER_API_KEY";
        public const string TimeoutSeconds = "TIMEOUT_SECONDS";
        public const string MaxRetries = "MAX_RETRIES";
        public const string InitialBackoffSeconds = "INITIAL_BACKOFF_SECONDS";
        public const string WebhookPort = "WEBHOOK_PORT";
        public const string LogLevel = "LOG_LEVEL";
    }

    public static class Operations
    {
        public const string Health = "health";
        public const string GetMedia = "getMedia";
        public const string DeleteMedia = "deleteMedia";
    }

    public static class MetricStatus
    {
        public const string Start = "start";
        public const string Ok = "ok";
        public const string Error = "error";
        public const string Exception = "exception";
        public const string Skipped = "skipped";
        public const string NotFound = "notfound";
    }
}
