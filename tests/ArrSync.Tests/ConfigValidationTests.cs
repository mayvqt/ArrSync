using ArrSync.App.Configuration;
using ArrSync.App.Models;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ArrSync.Tests;

public class ConfigValidationTests {
    [Fact]
    public void Validate_Fails_WhenApiKeyMissingInProduction() {
        var env = new FakeEnv { EnvironmentName = "Production" };
        var v = new ConfigValidation(env);
        var cfg = new Config { OverseerUrl = "http://localhost:5055", ApiKey = null, TimeoutSeconds = 10, MonitorIntervalSeconds = 10 };
        var result = v.Validate(null, cfg);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Validate_Succeeds_WithValidConfig() {
        var env = new FakeEnv { EnvironmentName = "Development" };
        var v = new ConfigValidation(env);
        var cfg = new Config { OverseerUrl = "http://localhost:5055", ApiKey = null, TimeoutSeconds = 10, MonitorIntervalSeconds = 10 };
        var result = v.Validate(null, cfg);
        Assert.True(result.Succeeded);
    }

    private class FakeEnv : IHostEnvironment {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = string.Empty;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
