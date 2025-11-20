using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ArrSync.App.Models;
using ArrSync.App.Services.Clients;
using ArrSync.App.Services.Monitoring;
using ArrSync.Tests.TestUtils;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ArrSync.Tests;

public static class TaskExtensions {
    public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken ct) {
        using var reg = ct.Register(() => { });
        return await task.ConfigureAwait(false);
    }
}

public class OverseerMonitorEndToEndTests {
    [Fact]
    public async Task Monitor_RequestsAdaptiveDelays_BasedOnFailures() {
        var calls = 0;
        var fakeOverseer = new FakeOverseerClient(() => {
            calls++;
            if (calls <= 2) {
                return Task.FromResult((false, "down"));
            }

            return Task.FromResult((true, "ok"));
        });

        var cfg = Options.Create(new Config { MonitorIntervalSeconds = 1 });
        var logger = NullLogger<OverseerMonitorService>.Instance;
        var timerFactory = new FakePeriodicTimerFactory();

        var svc = new OverseerMonitorService(fakeOverseer, cfg, logger, timerFactory);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var run = svc.RunAsync(cts.Token);

        await Task.Delay(1100);


        var created = 0;
        var sw = Stopwatch.StartNew();
        while (created == 0 && sw.Elapsed < TimeSpan.FromSeconds(2)) {
            created = timerFactory.CreatedTimers.Length;
            if (created == 0) {
                await Task.Delay(10);
            }
        }

        timerFactory.TickNext();
        await Task.Delay(10);
        timerFactory.TickNext();
        await Task.Delay(10);
        timerFactory.TickNext();

        cts.Cancel();
        await run;

        Assert.True(timerFactory.RecordedIntervals.Length >= 3);
        Assert.True(timerFactory.RecordedIntervals[1] >= timerFactory.RecordedIntervals[0]);
    }
}

internal class FakeOverseerClient : IOverseerClient {
    private readonly Func<Task<(bool, string)>> _responder;

    public FakeOverseerClient(Func<Task<(bool, string)>> responder) {
        _responder = responder;
    }

    public Task<(bool ok, string details)> HealthCheckAsync(CancellationToken ct) {
        return _responder();
    }

    public Task<int?> GetMediaIdByTmdbAsync(int tmdbId, string mediaType, CancellationToken ct) {
        return Task.FromResult<int?>(null);
    }

    public Task<bool> DeleteMediaAsync(int id, CancellationToken ct) {
        return Task.FromResult(true);
    }

    public Task<bool> IsAvailableAsync() {
        return Task.FromResult(true);
    }
}
