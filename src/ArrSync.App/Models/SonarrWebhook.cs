using System.Text.Json.Serialization;

namespace ArrSync.App.Models;

public class SonarrWebhook
{
    [JsonPropertyName("eventType")] public string? EventType { get; set; }
    [JsonPropertyName("instanceName")] public string? InstanceName { get; set; }
    [JsonPropertyName("series")] public SonarrSeries? Series { get; set; }
}

public class SonarrSeries
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("tmdbId")] public int TmdbId { get; set; }
    [JsonPropertyName("added")] public DateTime? Added { get; set; }
    [JsonPropertyName("images")] public List<SonarrImage>? Images { get; set; }
}

public class SonarrImage
{
    [JsonPropertyName("coverType")] public string? CoverType { get; set; }
    [JsonPropertyName("url")] public string? URL { get; set; }
}