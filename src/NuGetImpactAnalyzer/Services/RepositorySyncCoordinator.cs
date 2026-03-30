using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

public sealed class RepositorySyncCoordinator : IRepositorySyncCoordinator
{
    private readonly IGitService _gitService;

    public RepositorySyncCoordinator(IGitService gitService)
    {
        _gitService = gitService;
    }

    /// <inheritdoc />
    public async Task SyncAllAsync(IReadOnlyCollection<Repo> repos, IApplicationLog log, CancellationToken cancellationToken = default)
    {
        var startStamp = Timestamp();
        log.AppendLine($"[{startStamp}] Starting sync for {repos.Count} repo(s)…");

        var progress = new Progress<string>(log.AppendLine);
        await _gitService.SyncAllAsync(repos, progress, cancellationToken).ConfigureAwait(false);

        var endStamp = Timestamp();
        log.AppendLine($"[{endStamp}] Sync batch finished.");
    }

    private static string Timestamp() => DateTime.Now.ToString("HH:mm:ss");
}
