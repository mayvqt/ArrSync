using System.Text.Json.Serialization;

namespace ArrSync.App.Models;

public class SonarrWebhook
{
    [JsonPropertyName("eventType")] public string? EventType { get; set; }

    [JsonPropertyName("series")] public SonarrSeries? Series { get; set; }
}

public class SonarrSeries
{
    [JsonPropertyName("tmdbId")] public int TmdbId { get; set; }
}