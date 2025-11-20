using System;
using ArrSync.App.Services.Monitoring;
using Xunit;

namespace ArrSync.Tests;

public class OverseerMonitorTests
{
    [Theory]
    [InlineData(1, 0, 1000)]
    [InlineData(2, 1, 4000)]
    [InlineData(5, 4, 25000)]
    public void ComputeNextDelay_BacksOffAsExpected(int baseSeconds, int failureCount, int expectedMs)
    {
        var baseInterval = TimeSpan.FromSeconds(baseSeconds);
        var next = OverseerMonitorService.ComputeNextDelay(baseInterval, failureCount);
        Assert.Equal(expectedMs, (int)next.TotalMilliseconds);
    }
}
