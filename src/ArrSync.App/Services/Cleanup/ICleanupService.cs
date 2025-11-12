using System.Threading;
using System.Threading.Tasks;

namespace ArrSync.App.Services.Cleanup;
    /// <summary>
    /// Contract for the cleanup service responsible for processing media deletion
    /// events coming from Sonarr/Radarr webhooks and synchronizing deletions with Overseerr.
    /// </summary>
    public interface ICleanupService
    {
        /// <summary>
        /// Process a Sonarr (TV series) deletion event.
        /// </summary>
        /// <param name="tmdbId">TMDB id of the series. Must be > 0.</param>
        /// <param name="ct">Cancellation token.</param>
        Task ProcessSonarrAsync(int tmdbId, CancellationToken ct);

        /// <summary>
        /// Process a Radarr (movie) deletion event.
        /// </summary>
        /// <param name="tmdbId">TMDB id of the movie. Must be > 0.</param>
        /// <param name="ct">Cancellation token.</param>
        Task ProcessRadarrAsync(int tmdbId, CancellationToken ct);
    }
