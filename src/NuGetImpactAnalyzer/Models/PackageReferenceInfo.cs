namespace NuGetImpactAnalyzer.Models;

public sealed class PackageReferenceInfo
{
    public required string Include { get; init; }

    public string? Version { get; init; }
}
