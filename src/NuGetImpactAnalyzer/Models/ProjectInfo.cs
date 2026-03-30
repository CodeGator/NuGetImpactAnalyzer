namespace NuGetImpactAnalyzer.Models;

public sealed class ProjectInfo
{
    public required string Name { get; init; }

    public required string FilePath { get; init; }

    /// <summary>
    /// From PropertyGroup Version or PackageVersion, when present.
    /// </summary>
    public string? PackageVersion { get; init; }

    public IReadOnlyList<PackageReferenceInfo> PackageReferences { get; init; } = [];

    public IReadOnlyList<string> ProjectReferences { get; init; } = [];
}
