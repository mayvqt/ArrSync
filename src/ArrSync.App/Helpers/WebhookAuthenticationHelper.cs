using ArrSync.App.Models;

namespace ArrSync.App.Helpers;

public static class WebhookAuthenticationHelper {
    public static bool IsAuthenticated(HttpRequest request, Config config) {
        if (string.IsNullOrWhiteSpace(config.WebhookSecret)) {
            return true;
        }

        if (!request.Headers.TryGetValue(Constants.Headers.WebhookSecret, out var headerValue)) {
            return false;
        }

        return headerValue == config.WebhookSecret;
    }

    public static bool IsTestEvent(string? eventType) {
        return string.Equals(eventType, Constants.EventTypes.Test, StringComparison.OrdinalIgnoreCase);
    }
}
