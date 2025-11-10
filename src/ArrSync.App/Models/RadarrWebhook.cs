using System.Text.Json.Serialization;

namespace ArrSync.App.Models;

public class RadarrWebhook
{
    [JsonPropertyName("eventType")] public string? EventType { get; set; }

    [JsonPropertyName("movie")] public RadarrMovie? Movie { get; set; }
}

public class RadarrMovie
{
    [JsonPropertyName("tmdbId")] public int TmdbId { get; set; }
}