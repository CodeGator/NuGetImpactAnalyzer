namespace NuGetImpactAnalyzer.Models;

public sealed class GraphNode
{
    public required string Name { get; init; }

    public required string RepoName { get; init; }

    /// <summary>
    /// Target node ids (see <see cref="GraphService.MakeNodeId"/>), each identifying a dependency.
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; init; } = [];

    /// <summary>
    /// Declared NuGet version for this project (from csproj Version / PackageVersion), when present.
    /// Used as the resolved version when evaluating package-reference edges that point at this node.
    /// </summary>
    public string? ResolvedPackageVersion { get; init; }

    /// <summary>
    /// For each dependency reached via <see cref="PackageReference"/>, the required version range string (may be null if missing).
    /// Dependencies reached only via <see cref="ProjectInfo.ProjectReferences"/> are omitted from this map.
    /// </summary>
    public IReadOnlyDictionary<string, string?> PackageDependencyConstraints { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}
