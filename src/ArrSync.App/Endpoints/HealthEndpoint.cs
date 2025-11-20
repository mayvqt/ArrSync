using ArrSync.App.Services.Clients;

namespace ArrSync.App.Endpoints;

public static class HealthEndpoint {
    public static void MapHealthEndpoint(this WebApplication app) {
        app.MapGet("/health", HandleHealthCheck)
            .WithName("HealthCheck")
            .WithTags("Health")
            .Produces<HealthResponse>()
            .Produces(503);
    }

    private static async Task<IResult> HandleHealthCheck(
        IOverseerClient overseerClient,
        CancellationToken cancellationToken) {
        var (ok, _) = await overseerClient.HealthCheckAsync(cancellationToken);
        var overseerStatus = await overseerClient.IsAvailableAsync() ? "available" : "unavailable";

        var response = new HealthResponse {
            Status = ok ? "healthy" : "degraded",
            Service = "arrsync",
            Healthy = ok,
            Overseer = overseerStatus
        };

        return Results.Json(response, statusCode: ok ? 200 : 503);
    }

    private record HealthResponse {
        public required string Status { get; init; }
        public required string Service { get; init; }
        public required bool Healthy { get; init; }
        public required string Overseer { get; init; }
    }
}
