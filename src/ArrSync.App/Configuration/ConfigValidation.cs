using ArrSync.App.Models;
using Microsoft.Extensions.Options;

namespace ArrSync.App.Configuration;

public class ConfigValidation : IValidateOptions<Config>
{
    private readonly IHostEnvironment _env;

    public ConfigValidation(IHostEnvironment env)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
    }

    public ValidateOptionsResult Validate(string? name, Config options)
    {
        var failures = new List<string>();

        if (options == null)
        {
            return ValidateOptionsResult.Fail("Config is null");
        }

        if (string.IsNullOrWhiteSpace(options.OverseerUrl) ||
            !Uri.TryCreate(options.OverseerUrl, UriKind.Absolute, out _))
        {
            failures.Add("OverseerUrl must be a valid absolute URI");
        }

        if (options.TimeoutSeconds < 1)
        {
            failures.Add("TimeoutSeconds must be >= 1");
        }

        if (options.MonitorIntervalSeconds < 1)
        {
            failures.Add("MonitorIntervalSeconds must be >= 1");
        }

        if (options.MaxRetries < 0)
        {
            failures.Add("MaxRetries must be >= 0");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey) &&
            string.Equals(_env.EnvironmentName, "Production", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("ApiKey must be set in Production environment");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
