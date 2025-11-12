using System.Text.Json.Serialization;

namespace ArrSync.App.Models;

/// <summary>
/// Webhook payload received from Sonarr.
/// </summary>
public sealed record SonarrWebhook
{
    [JsonPropertyName("eventType")] 
    public string? EventType { get; init; }
    
    [JsonPropertyName("instanceName")] 
    public string? InstanceName { get; init; }
    
    [JsonPropertyName("series")] 
    public SonarrSeries? Series { get; init; }
}

/// <summary>
/// Series information from Sonarr webhook.
/// </summary>
public sealed record SonarrSeries
{
    [JsonPropertyName("id")] 
    public int Id { get; init; }
    
    [JsonPropertyName("title")] 
    public string? Title { get; init; }
    
    [JsonPropertyName("tmdbId")] 
    public int TmdbId { get; init; }
    
    [JsonPropertyName("added")] 
    public DateTime? Added { get; init; }
    
    [JsonPropertyName("images")] 
    public IReadOnlyList<SonarrImage>? Images { get; init; }
}

/// <summary>
/// Image information from Sonarr series.
/// </summary>
public sealed record SonarrImage
{
    [JsonPropertyName("coverType")] 
    public string? CoverType { get; init; }
    
    [JsonPropertyName("url")] 
    public string? URL { get; init; }
}
