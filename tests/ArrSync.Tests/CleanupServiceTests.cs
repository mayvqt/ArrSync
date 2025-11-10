using System.Threading;
using System.Threading.Tasks;
using ArrSync.App.Models;
using ArrSync.App.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ArrSync.Tests;

public class CleanupServiceTests
{
    [Fact]
    public async Task ProcessSonarr_DryRun_LogsAndNoDelete()
    {
        var fake = new FakeOverseerClient();
    var opts = Options.Create(new ArrSync.App.Models.Config { DryRun = true });
        var svc = new CleanupService(fake, opts, NullLogger<CleanupService>.Instance);

        await svc.ProcessSonarrAsync(123, CancellationToken.None);

        fake.DeleteCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessRadarr_When_NoMediaFound_DoesNotDelete()
    {
        var fake = new FakeOverseerClient();
        fake.NextMediaId = null;
    var opts = Options.Create(new ArrSync.App.Models.Config { DryRun = false });
        var svc = new CleanupService(fake, opts, NullLogger<CleanupService>.Instance);

        await svc.ProcessRadarrAsync(456, CancellationToken.None);

        fake.DeleteCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessRadarr_When_MediaFound_Deletes()
    {
        var fake = new FakeOverseerClient();
        fake.NextMediaId = 77;
    var opts = Options.Create(new ArrSync.App.Models.Config { DryRun = false });
        var svc = new CleanupService(fake, opts, NullLogger<CleanupService>.Instance);

        await svc.ProcessRadarrAsync(456, CancellationToken.None);

        fake.DeleteCalled.Should().BeTrue();
        fake.DeletedId.Should().Be(77);
    }
}

internal class FakeOverseerClient : IOverseerClient
{
    public int? NextMediaId { get; set; } = 42;
    public bool DeleteCalled { get; private set; }
    public int DeletedId { get; private set; }

    public Task<(bool ok, string details)> HealthCheckAsync(CancellationToken ct)
        => Task.FromResult((true, "ok"));

    public Task<int?> GetMediaIdByTmdbAsync(int tmdbId, string mediaType, CancellationToken ct)
        => Task.FromResult(NextMediaId);

    public Task<bool> DeleteMediaAsync(int id, CancellationToken ct)
    {
        DeleteCalled = true;
        DeletedId = id;
        return Task.FromResult(true);
    }

    public bool IsAvailable() => true;
}
