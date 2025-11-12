using System;
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

        [Fact]
        public void SonarrWebhook_Deserializes_Full()
        {
                var json = @"{
            ""eventType"": ""SeriesDelete"",
            ""instanceName"": ""sonarr"",
            ""series"": {
                ""id"": 2,
                ""title"": ""My Show"",
                ""tmdbId"": 101,
                ""added"": ""2020-01-02T15:04:05Z"",
                ""images"": [{""coverType"":""poster"",""url"":""http://example.com/poster.jpg""}]
            }
        }";
                var obj = JsonSerializer.Deserialize<SonarrWebhook>(json);
                obj.Should().NotBeNull();
                obj!.EventType.Should().Be("SeriesDelete");
                obj.InstanceName.Should().Be("sonarr");
                obj.Series.Should().NotBeNull();
                obj.Series!.TmdbId.Should().Be(101);
                obj.Series.Added.Should().Be(DateTime.Parse("2020-01-02T15:04:05Z").ToUniversalTime());
                obj.Series.Images.Should().HaveCount(1);
                obj.Series.Images![0].URL.Should().Be("http://example.com/poster.jpg");
        }

        [Fact]
        public void RadarrWebhook_Deserializes_Full()
        {
                var json = @"{
            ""eventType"": ""MovieDelete"",
            ""instanceName"": ""radarr"",
            ""movie"": {
                ""id"": 1,
                ""title"": ""My Movie"",
                ""tmdbId"": 42,
                ""year"": 2020,
                ""hasFile"": false
            }
        }";
                var obj = JsonSerializer.Deserialize<RadarrWebhook>(json);
                obj.Should().NotBeNull();
                obj!.EventType.Should().Be("MovieDelete");
                obj.InstanceName.Should().Be("radarr");
                obj.Movie.Should().NotBeNull();
                obj.Movie!.TmdbId.Should().Be(42);
                obj.Movie.Year.Should().Be(2020);
                obj.Movie.HasFile.Should().BeFalse();
        }
}
