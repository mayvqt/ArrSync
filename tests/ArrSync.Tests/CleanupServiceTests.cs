using System.Threading;
using System.Threading.Tasks;
using ArrSync.App.Models;
using ArrSync.App.Services.Cleanup;
using ArrSync.App.Services.Clients;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ArrSync.Tests;

public class CleanupServiceTests {
    [Fact]
    public async Task DryRun_DoesNotCallOverseer() {
        var fake = new FakeOverseer();
        var cfg = Options.Create(new Config { DryRun = true });
        var svc = new CleanupService(fake, cfg, NullLogger<CleanupService>.Instance);

        await svc.ProcessRadarrAsync(1234, CancellationToken.None);
        Assert.Null(fake.LastRequestedTmdb);
    }

    [Fact]
    public async Task ProcessRadarr_CallsOverseer_WhenNotDryRun() {
        var fake = new FakeOverseer { ToReturnId = 42 };
        var cfg = Options.Create(new Config { DryRun = false });
        var svc = new CleanupService(fake, cfg, NullLogger<CleanupService>.Instance);

        await svc.ProcessRadarrAsync(555, CancellationToken.None);
        Assert.Equal(555, fake.LastRequestedTmdb);
    }

    private class FakeOverseer : IOverseerClient {
        public int? LastRequestedTmdb { get; private set; }
        public int? ToReturnId { get; set; }

        public Task<bool> IsAvailableAsync() {
            return Task.FromResult(true);
        }

        public Task<(bool ok, string details)> HealthCheckAsync(CancellationToken ct) {
            return Task.FromResult((true, "ok"));
        }

        public Task<int?> GetMediaIdByTmdbAsync(int tmdbId, string mediaType, CancellationToken ct) {
            LastRequestedTmdb = tmdbId;
            return Task.FromResult(ToReturnId);
        }

        public Task<bool> DeleteMediaAsync(int id, CancellationToken ct) {
            return Task.FromResult(true);
        }
    }
}
