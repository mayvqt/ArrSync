using ArrSync.App.Models;
using Microsoft.Extensions.Options;

namespace ArrSync.App.Services;

public class CleanupService
{
    private readonly ILogger<CleanupService> _log;
    private readonly Config _opts;
    private readonly IOverseerClient _overseer;

    public CleanupService(IOverseerClient overseer, IOptions<Config> opts, ILogger<CleanupService> log)
    {
        _overseer = overseer;
        _opts = opts.Value;
        _log = log;
    }

    public async Task ProcessSonarrAsync(int tmdbId, CancellationToken ct)
    {
        // Sonarr series -> type "tv"
        await ProcessAsync(tmdbId, "tv", ct);
    }

    public async Task ProcessRadarrAsync(int tmdbId, CancellationToken ct)
    {
        // Radarr movie -> type "movie"
        await ProcessAsync(tmdbId, "movie", ct);
    }

    private async Task ProcessAsync(int tmdbId, string mediaType, CancellationToken ct)
    {
        if (_opts.DryRun)
        {
            _log.LogWarning("[DRY_RUN] Would process tmdb {tmdbId} type {mediaType}", tmdbId, mediaType);
            return;
        }

        var id = await _overseer.GetMediaIdByTmdbAsync(tmdbId, mediaType, ct);
        if (id == null)
        {
            _log.LogInformation("No media found for tmdb={tmdbId} type={mediaType}", tmdbId, mediaType);
            return;
        }

        var ok = await _overseer.DeleteMediaAsync(id.Value, ct);
        if (ok)
            _log.LogInformation("Deleted media id={mediaId}", id.Value);
        else
            _log.LogError("Failed to delete media id={mediaId}", id.Value);
    }
}