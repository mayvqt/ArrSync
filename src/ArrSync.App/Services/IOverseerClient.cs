namespace ArrSync.App.Services;

public interface IOverseerClient
{
    Task<(bool ok, string details)> HealthCheckAsync(CancellationToken ct);
    Task<int?> GetMediaIdByTmdbAsync(int tmdbId, string mediaType, CancellationToken ct);
    Task<bool> DeleteMediaAsync(int id, CancellationToken ct);
    Task<bool> IsAvailableAsync();
}