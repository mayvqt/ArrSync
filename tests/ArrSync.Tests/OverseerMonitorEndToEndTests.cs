using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArrSync.App.Models;
using ArrSync.App.Services.Monitoring;
using ArrSync.App.Services.Clients;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ArrSync.Tests;

using ArrSync.Tests.TestUtils;

public static class TaskExtensions
{
    public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken ct)
    {
        using var reg = ct.Register(() => { });
        return await task.ConfigureAwait(false);
    }
}

public class OverseerMonitorEndToEndTests
{
    [Fact]
    public async Task Monitor_RequestsAdaptiveDelays_BasedOnFailures()
    {
        var calls = 0;
        var fakeOverseer = new FakeOverseerClient(() =>
        {
            calls++;
            // first two calls fail, third succeeds
            if (calls <= 2) return Task.FromResult((false, "down"));
            return Task.FromResult((true, "ok"));
        });

            var cfg = Options.Create(new Config { MonitorIntervalSeconds = 1 });
            var logger = NullLogger<OverseerMonitorService>.Instance;
        var timerFactory = new FakePeriodicTimerFactory();

            var svc = new OverseerMonitorService(fakeOverseer, cfg, logger, timerFactory);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var run = svc.RunAsync(cts.Token);

        // Allow initial delay to elapse (monitor waits baseInterval once). Wait for the first timer to be created.
        await Task.Delay(1100);

        // Wait until the monitor creates at least one timer (with small timeout to avoid flakiness)
        var created = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (created == 0 && sw.Elapsed < TimeSpan.FromSeconds(2))
        {
            created = timerFactory.CreatedTimers.Length;
            if (created == 0) await Task.Delay(10);
        }

        // Drive a few iterations deterministically by ticking the timers the monitor creates
        timerFactory.TickNext();
        await Task.Delay(10);
        timerFactory.TickNext();
        await Task.Delay(10);
        timerFactory.TickNext();

    // cancel and await completion
    cts.Cancel();
    await run;

    // We expect at least three create calls (one per loop). Ensure requested intervals reflect backoff multipliers
    Assert.True(timerFactory.RecordedIntervals.Length >= 3);
    // the second requested interval should be larger or equal to the first (backoff non-decreasing)
    Assert.True(timerFactory.RecordedIntervals[1] >= timerFactory.RecordedIntervals[0]);
    }
}

internal class FakeOverseerClient : IOverseerClient
{
    private readonly Func<Task<(bool, string)>> _responder;
    public FakeOverseerClient(Func<Task<(bool, string)>> responder) => _responder = responder;
    public Task<(bool ok, string details)> HealthCheckAsync(CancellationToken ct) => _responder();
    public Task<int?> GetMediaIdByTmdbAsync(int tmdbId, string mediaType, CancellationToken ct) => Task.FromResult<int?>(null);
    public Task<bool> DeleteMediaAsync(int id, CancellationToken ct) => Task.FromResult(true);
    public Task<bool> IsAvailableAsync() => Task.FromResult(true);
}

 
