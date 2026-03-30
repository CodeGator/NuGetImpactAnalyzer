using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Orchestrates Git sync for the configured repository list and status logging.
/// </summary>
public interface IRepositorySyncCoordinator
{
    Task SyncAllAsync(IReadOnlyCollection<Repo> repos, IApplicationLog log, CancellationToken cancellationToken = default);
}
