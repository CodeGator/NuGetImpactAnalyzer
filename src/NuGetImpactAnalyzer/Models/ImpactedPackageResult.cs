namespace NuGetImpactAnalyzer.Models;

/// <summary>
/// One transitive dependent discovered by impact analysis, with semantic severity.
/// </summary>
public sealed class ImpactedPackageResult
{
    public required string NodeId { get; init; }

    public required string DisplayLabel { get; init; }

    public required ImpactSeverity Severity { get; init; }
}
