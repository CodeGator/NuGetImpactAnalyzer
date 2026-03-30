namespace NuGetImpactAnalyzer.Models;

/// <summary>
/// Root JSON shape for <c>Data/cache.json</c> (parsed project snapshots per repo + commit).
/// </summary>
public sealed class ParseCacheDocument
{
    public List<ParseCacheRecord> Entries { get; init; } = [];
}

public sealed class ParseCacheRecord
{
    public string RepoName { get; init; } = "";

    public string CommitHash { get; init; } = "";

    public List<ProjectInfo> Projects { get; init; } = [];
}
