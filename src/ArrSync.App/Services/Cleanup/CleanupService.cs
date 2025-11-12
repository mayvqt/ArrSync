using ArrSync.App.Models;
using Microsoft.Extensions.Options;
using ArrSync.App.Services.Clients;

namespace ArrSync.App.Services.Cleanup;

/// <summary>
/// Service responsible for processing media deletion requests from Sonarr/Radarr webhooks
/// and synchronizing deletions with Overseerr.
/// </summary>
public sealed class CleanupService : ICleanupService
{
    private readonly ILogger<CleanupService> _log;
    private readonly Config _opts;
    private readonly IOverseerClient _overseer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupService"/> class.
    /// </summary>
    /// <param name="overseer">The Overseerr client for API operations.</param>
    /// <param name="opts">Configuration options.</param>
    /// <param name="log">Logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public CleanupService(IOverseerClient overseer, IOptions<Config> opts, ILogger<CleanupService> log)
    {
        ArgumentNullException.ThrowIfNull(overseer);
        ArgumentNullException.ThrowIfNull(opts);
        ArgumentNullException.ThrowIfNull(log);

        _overseer = overseer;
        _opts = opts.Value;
        _log = log;
    }

    /// <summary>
    /// Processes a Sonarr webhook for TV series deletion.
    /// </summary>
    /// <param name="tmdbId">The TMDB ID of the TV series.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when tmdbId is less than or equal to 0.</exception>
    public async Task ProcessSonarrAsync(int tmdbId, CancellationToken ct)
    {
        if (tmdbId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tmdbId), tmdbId, "TMDB ID must be greater than 0");

        ct.ThrowIfCancellationRequested();
        await ProcessMediaDeletionAsync(tmdbId, Constants.MediaTypes.Tv, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Processes a Radarr webhook for movie deletion.
    /// </summary>
    /// <param name="tmdbId">The TMDB ID of the movie.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when tmdbId is less than or equal to 0.</exception>
    public async Task ProcessRadarrAsync(int tmdbId, CancellationToken ct)
    {
        if (tmdbId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tmdbId), tmdbId, "TMDB ID must be greater than 0");

        ct.ThrowIfCancellationRequested();
        await ProcessMediaDeletionAsync(tmdbId, Constants.MediaTypes.Movie, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Core logic for processing media deletion: lookup by TMDB ID, then delete from Overseerr.
    /// </summary>
    /// <param name="tmdbId">The TMDB ID of the media.</param>
    /// <param name="mediaType">The media type (tv or movie).</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task ProcessMediaDeletionAsync(int tmdbId, string mediaType, CancellationToken ct)
    {
        if (_opts.DryRun)
        {
            _log.LogWarning("[DRY_RUN] Would process media deletion for tmdbId={TmdbId}, type={MediaType}", 
                tmdbId, mediaType);
            return;
        }
        try
        {
            ct.ThrowIfCancellationRequested();

            var mediaId = await _overseer.GetMediaIdByTmdbAsync(tmdbId, mediaType, ct).ConfigureAwait(false);
            if (mediaId == null)
            {
                _log.LogInformation("No media found in Overseerr for tmdbId={TmdbId}, type={MediaType}",
                    tmdbId, mediaType);
                return;
            }

            ct.ThrowIfCancellationRequested();
            var deleted = await _overseer.DeleteMediaAsync(mediaId.Value, ct).ConfigureAwait(false);
            if (deleted)
            {
                _log.LogInformation("Successfully deleted media from Overseerr: id={MediaId}, tmdbId={TmdbId}, type={MediaType}",
                    mediaId.Value, tmdbId, mediaType);
            }
            else
            {
                _log.LogError("Failed to delete media from Overseerr: id={MediaId}, tmdbId={TmdbId}, type={MediaType}",
                    mediaId.Value, tmdbId, mediaType);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected to flow through; log at debug level and rethrow to respect caller
            _log.LogDebug("ProcessMediaDeletionAsync was canceled for tmdbId={TmdbId}, type={MediaType}", tmdbId, mediaType);
            throw;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Exception while processing media deletion for tmdbId={TmdbId}, type={MediaType}",
                tmdbId, mediaType);
            throw;
        }
    }
}
