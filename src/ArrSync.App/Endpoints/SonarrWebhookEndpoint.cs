using ArrSync.App.Helpers;
using ArrSync.App.Models;
using ArrSync.App.Services.Cleanup;
using Microsoft.Extensions.Options;

namespace ArrSync.App.Endpoints;

/// <summary>
/// Handler for Sonarr webhook endpoint.
/// </summary>
public static class SonarrWebhookEndpoint
{
    /// <summary>
    /// Configures the Sonarr webhook endpoint.
    /// </summary>
    public static void MapSonarrWebhookEndpoint(this WebApplication app)
    {
        app.MapPost("/webhook/sonarr", HandleSonarrWebhook)
            .WithName("SonarrWebhook")
            .WithTags("Webhooks")
            .Produces<WebhookResponse>(200)
            .Produces(401);
    }

    private static async Task<IResult> HandleSonarrWebhook(
        HttpRequest request,
    SonarrWebhook payload,
    ICleanupService cleanupService,
        IOptions<Config> config,
        CancellationToken cancellationToken)
    {
        // Authenticate webhook request
        if (!WebhookAuthenticationHelper.IsAuthenticated(request, config.Value))
            return Results.Unauthorized();

        // Ignore test events
        if (WebhookAuthenticationHelper.IsTestEvent(payload.EventType))
            return Results.Ok(new WebhookResponse { Message = "test event ignored" });

        // Validate TMDB ID
        var tmdbId = payload.Series?.TmdbId ?? 0;
        if (tmdbId <= 0)
            return Results.Ok(new WebhookResponse { Message = "no tmdb id found" });

        // Process the webhook
        await cleanupService.ProcessSonarrAsync(tmdbId, cancellationToken);
        return Results.Ok(new WebhookResponse { Message = "processed" });
    }

    private record WebhookResponse
    {
        public required string Message { get; init; }
    }
}
