using System.Diagnostics.CodeAnalysis;
using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Persists parsed <see cref="ProjectInfo"/> lists keyed by repository name and HEAD commit SHA.
/// </summary>
public interface IParseResultCache
{
    /// <summary>
    /// Returns cached projects when <paramref name="commitSha"/> matches the stored entry for <paramref name="repoName"/>.
    /// </summary>
    bool TryGet(string repoName, string commitSha, [NotNullWhen(true)] out IReadOnlyList<ProjectInfo>? projects);

    /// <summary>
    /// Stores or replaces the cache entry for <paramref name="repoName"/> at <paramref name="commitSha"/>.
    /// </summary>
    void Save(string repoName, string commitSha, IReadOnlyList<ProjectInfo> projects);

    /// <summary>
    /// Removes all cached parse results for <paramref name="repoName"/> (case-insensitive trim on name).
    /// </summary>
    void RemoveEntriesForRepository(string repoName);
}
