using ArrSync.App.Models;
using ArrSync.App.Services.Clients;
using Microsoft.Extensions.Options;

namespace ArrSync.App.Services.Cleanup;

public sealed class CleanupService : ICleanupService {
    private readonly ILogger<CleanupService> _log;
    private readonly Config _opts;
    private readonly IOverseerClient _overseer;

    public CleanupService(IOverseerClient overseer, IOptions<Config> opts, ILogger<CleanupService> log) {
        ArgumentNullException.ThrowIfNull(overseer);
        ArgumentNullException.ThrowIfNull(opts);
        ArgumentNullException.ThrowIfNull(log);

        _overseer = overseer;
        _opts = opts.Value;
        _log = log;
    }

    public async Task ProcessSonarrAsync(int tmdbId, CancellationToken ct) {
        if (tmdbId <= 0) {
            throw new ArgumentOutOfRangeException(nameof(tmdbId), tmdbId, "TMDB ID must be greater than 0");
        }

        ct.ThrowIfCancellationRequested();
        await ProcessMediaDeletionAsync(tmdbId, Constants.MediaTypes.Tv, ct).ConfigureAwait(false);
    }

    public async Task ProcessRadarrAsync(int tmdbId, CancellationToken ct) {
        if (tmdbId <= 0) {
            throw new ArgumentOutOfRangeException(nameof(tmdbId), tmdbId, "TMDB ID must be greater than 0");
        }

        ct.ThrowIfCancellationRequested();
        await ProcessMediaDeletionAsync(tmdbId, Constants.MediaTypes.Movie, ct).ConfigureAwait(false);
    }

    private async Task ProcessMediaDeletionAsync(int tmdbId, string mediaType, CancellationToken ct) {
        if (_opts.DryRun) {
            _log.LogWarning("[DRY_RUN] Would process media deletion for tmdbId={TmdbId}, type={MediaType}",
                tmdbId, mediaType);
            return;
        }

        try {
            ct.ThrowIfCancellationRequested();

            var mediaId = await _overseer.GetMediaIdByTmdbAsync(tmdbId, mediaType, ct).ConfigureAwait(false);
            if (mediaId == null) {
                _log.LogInformation("No media found in Overseerr for tmdbId={TmdbId}, type={MediaType}",
                    tmdbId, mediaType);
                return;
            }

            ct.ThrowIfCancellationRequested();
            var deleted = await _overseer.DeleteMediaAsync(mediaId.Value, ct).ConfigureAwait(false);
            if (deleted) {
                _log.LogInformation(
                    "Successfully deleted media from Overseerr: id={MediaId}, tmdbId={TmdbId}, type={MediaType}",
                    mediaId.Value, tmdbId, mediaType);
            }
            else {
                _log.LogError("Failed to delete media from Overseerr: id={MediaId}, tmdbId={TmdbId}, type={MediaType}",
                    mediaId.Value, tmdbId, mediaType);
            }
        }
        catch (OperationCanceledException) {
            _log.LogDebug("ProcessMediaDeletionAsync was canceled for tmdbId={TmdbId}, type={MediaType}", tmdbId,
                mediaType);
            throw;
        }
        catch (Exception ex) {
            _log.LogError(ex, "Exception while processing media deletion for tmdbId={TmdbId}, type={MediaType}",
                tmdbId, mediaType);
            throw;
        }
    }
}
