using System.Text.Json.Serialization;

namespace ArrSync.App.Models;

public sealed record RadarrWebhook
{
    [JsonPropertyName("eventType")] public string? EventType { get; init; }

    [JsonPropertyName("instanceName")] public string? InstanceName { get; init; }

    [JsonPropertyName("movie")] public RadarrMovie? Movie { get; init; }
}

public sealed record RadarrMovie
{
    [JsonPropertyName("id")] public int Id { get; init; }

    [JsonPropertyName("title")] public string? Title { get; init; }

    [JsonPropertyName("tmdbId")] public int TmdbId { get; init; }

    [JsonPropertyName("year")] public int Year { get; init; }

    [JsonPropertyName("hasFile")] public bool HasFile { get; init; }
}
