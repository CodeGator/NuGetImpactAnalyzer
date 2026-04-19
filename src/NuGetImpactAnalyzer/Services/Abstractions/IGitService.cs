using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

public interface IGitService
{
    /// <summary>
    /// Absolute path to the root folder that holds per-repository clone directories (typically under Data/repos).
    /// </summary>
    string GetRepositoriesRoot();

    /// <summary>
    /// Absolute path to the local clone directory for this repository (typically under Data/).
    /// </summary>
    string GetLocalRepositoryPath(Repo repo);

    /// <summary>
    /// Returns whether a valid local Git repository exists at <see cref="GetLocalRepositoryPath"/>.
    /// </summary>
    bool IsLocalClonePresent(Repo repo);

    /// <summary>
    /// Returns the current HEAD commit SHA for a valid local repository, or null if missing or empty.
    /// </summary>
    string? TryGetHeadCommitSha(string localRepositoryPath);

    /// <summary>
    /// Deletes the local clone directory for this repository if it exists.
    /// </summary>
    /// <returns><see langword="true"/> if the path was absent or removed successfully; <see langword="false"/> if deletion failed.</returns>
    bool TryDeleteLocalClone(Repo repo);

    /// <summary>
    /// Clones the repository into Data/{repo name}, or pulls the latest changes if it already exists locally.
    /// </summary>
    Task CloneOrUpdateAsync(Repo repo, IProgress<string>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <see cref="CloneOrUpdateAsync"/> for each repository. Individual failures are reported; other repos continue.
    /// </summary>
    Task SyncAllAsync(IEnumerable<Repo> repos, IProgress<string>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <see langword="true"/> if <c>git ls-remote</c> can read refs from <see cref="Repo.Url"/> (same transport as <see cref="ListBranches"/>).
    /// Use when GitHub REST API checks fail (e.g. rate limit) but the remote is still reachable over HTTPS/SSH.
    /// </summary>
    bool TryProbeRemoteRepository(Repo repo);

    /// <summary>
    /// Lists branch names: from the local clone if present, otherwise via <c>ls-remote</c> (requires URL and optional credentials).
    /// </summary>
    IReadOnlyList<string> ListBranches(Repo repo);

    /// <summary>
    /// Paths to *.csproj files under the local clone, relative to the repository root with forward slashes; empty if not cloned.
    /// </summary>
    IReadOnlyList<string> ListProjectRelativePaths(Repo repo);
}
