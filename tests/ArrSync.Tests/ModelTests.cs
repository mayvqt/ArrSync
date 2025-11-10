using System.Text.Json;
using ArrSync.App.Models;
using FluentAssertions;
using Xunit;

namespace ArrSync.Tests;

public class ModelTests
{
    [Fact]
    public void SonarrWebhook_Deserializes_Minimal()
    {
        var json = "{ \"eventType\": \"SeriesDelete\", \"series\": { \"tmdbId\": 123 } }";
        var obj = JsonSerializer.Deserialize<SonarrWebhook>(json);
        obj.Should().NotBeNull();
        obj!.EventType.Should().Be("SeriesDelete");
        obj.Series!.TmdbId.Should().Be(123);
    }

    [Fact]
    public void RadarrWebhook_Deserializes_Minimal()
    {
        var json = "{ \"eventType\": \"MovieDelete\", \"movie\": { \"tmdbId\": 456 } }";
        var obj = JsonSerializer.Deserialize<RadarrWebhook>(json);
        obj.Should().NotBeNull();
        obj!.EventType.Should().Be("MovieDelete");
        obj.Movie!.TmdbId.Should().Be(456);
    }
}
