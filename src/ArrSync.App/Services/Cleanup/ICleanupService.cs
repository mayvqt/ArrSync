namespace ArrSync.App.Services.Cleanup;

public interface ICleanupService
{
    Task ProcessSonarrAsync(int tmdbId, CancellationToken ct);

    Task ProcessRadarrAsync(int tmdbId, CancellationToken ct);
}
