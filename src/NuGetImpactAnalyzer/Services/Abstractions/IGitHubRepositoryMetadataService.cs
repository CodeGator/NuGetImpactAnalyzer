using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Looks up repository metadata from the GitHub REST API (e.g. public vs private).
/// </summary>
public interface IGitHubRepositoryMetadataService
{
    /// <summary>
    /// For each repo, queries api.github.com when the URL is a GitHub repo; otherwise returns null.
    /// Uses the stored token for <see cref="Repo.Name"/> when present (needed for private repos).
    /// </summary>
    Task<IReadOnlyList<(Repo Repo, bool? GitHubReportsPrivate)>> GetGitHubPrivateFlagsAsync(
        IReadOnlyList<Repo> repos,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures <paramref name="url"/> is a github.com repository URL and that the GitHub REST API can resolve it
    /// (uses stored PAT for <paramref name="credentialLookupName"/> when needed for private repos).
    /// </summary>
    Task<GitHubRepositoryUrlValidationResult> ValidateGitHubRepositoryUrlAsync(
        string url,
        string credentialLookupName,
        CancellationToken cancellationToken = default);
}
