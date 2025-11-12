using System.Text.Json.Serialization;

namespace ArrSync.App.Models;

public class RadarrWebhook
{
    [JsonPropertyName("eventType")] public string? EventType { get; set; }
    [JsonPropertyName("instanceName")] public string? InstanceName { get; set; }
    [JsonPropertyName("movie")] public RadarrMovie? Movie { get; set; }
}

public class RadarrMovie
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("tmdbId")] public int TmdbId { get; set; }
    [JsonPropertyName("year")] public int Year { get; set; }
    [JsonPropertyName("hasFile")] public bool HasFile { get; set; }
}