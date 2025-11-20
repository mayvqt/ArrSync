using ArrSync.App.Helpers;
using ArrSync.App.Models;
using ArrSync.App.Services.Cleanup;
using Microsoft.Extensions.Options;

namespace ArrSync.App.Endpoints;

public static class RadarrWebhookEndpoint {
    public static void MapRadarrWebhookEndpoint(this WebApplication app) {
        app.MapPost("/webhook/radarr", HandleRadarrWebhook)
            .WithName("RadarrWebhook")
            .WithTags("Webhooks")
            .Produces<WebhookResponse>()
            .Produces(401);
    }

    private static async Task<IResult> HandleRadarrWebhook(
        HttpRequest request,
        RadarrWebhook payload,
        ICleanupService cleanupService,
        IOptions<Config> config,
        CancellationToken cancellationToken) {
        if (!WebhookAuthenticationHelper.IsAuthenticated(request, config.Value)) {
            return Results.Unauthorized();
        }

        if (WebhookAuthenticationHelper.IsTestEvent(payload.EventType)) {
            return Results.Ok(new WebhookResponse { Message = "test event ignored" });
        }

        var tmdbId = payload.Movie?.TmdbId ?? 0;
        if (tmdbId <= 0) {
            return Results.Ok(new WebhookResponse { Message = "no tmdb id found" });
        }

        await cleanupService.ProcessRadarrAsync(tmdbId, cancellationToken);
        return Results.Ok(new WebhookResponse { Message = "processed" });
    }

    private record WebhookResponse {
        public required string Message { get; init; }
    }
}
