using System.Threading;
using System.Threading.Tasks;

namespace ArrSync.App.Services.Clients;
    /// <summary>
    /// Canonical interface for the Overseerr client used by application services.
    /// Placed in the Clients folder and namespace to align with physical layout.
    /// </summary>
    public interface IOverseerClient
    {
        Task<(bool ok, string details)> HealthCheckAsync(CancellationToken ct);
        Task<int?> GetMediaIdByTmdbAsync(int tmdbId, string mediaType, CancellationToken ct);
        Task<bool> DeleteMediaAsync(int id, CancellationToken ct);
        Task<bool> IsAvailableAsync();
    }

