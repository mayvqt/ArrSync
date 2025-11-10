using System.Text.Json.Serialization;

namespace ArrSync.App.Models;

public class OverseerMedia
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("title")] public string? Title { get; set; }

    [JsonPropertyName("mediaInfo")] public OverseerMediaInfo? MediaInfo { get; set; }
}

public class OverseerMediaInfo
{
    [JsonPropertyName("id")] public int Id { get; set; }
}