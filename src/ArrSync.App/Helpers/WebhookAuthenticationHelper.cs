using ArrSync.App.Models;
using Microsoft.Extensions.Options;

namespace ArrSync.App.Helpers;

/// <summary>
/// Helper class for validating webhook authentication.
/// </summary>
public static class WebhookAuthenticationHelper
{
    /// <summary>
    /// Validates the webhook secret from the request headers.
    /// </summary>
    /// <param name="request">The HTTP request to validate.</param>
    /// <param name="config">The application configuration.</param>
    /// <returns>True if authentication is valid or not required; false otherwise.</returns>
    public static bool IsAuthenticated(HttpRequest request, Config config)
    {
        // If no secret is configured, authentication is not required
        if (string.IsNullOrWhiteSpace(config.WebhookSecret))
        {
            return true;
        }

        // Check if the request contains the webhook secret header
        if (!request.Headers.TryGetValue(Constants.Headers.WebhookSecret, out var headerValue))
        {
            return false;
        }

        // Validate the secret matches
        return headerValue == config.WebhookSecret;
    }

    /// <summary>
    /// Validates if the event type is a test event that should be ignored.
    /// </summary>
    /// <param name="eventType">The event type to check.</param>
    /// <returns>True if this is a test event; false otherwise.</returns>
    public static bool IsTestEvent(string? eventType)
    {
        return string.Equals(eventType, Constants.EventTypes.Test, StringComparison.OrdinalIgnoreCase);
    }
}
