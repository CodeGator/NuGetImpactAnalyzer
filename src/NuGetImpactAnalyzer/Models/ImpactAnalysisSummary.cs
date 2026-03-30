namespace NuGetImpactAnalyzer.Models;

/// <summary>
/// Result of impact analysis + graph highlight for UI binding.
/// </summary>
public sealed class ImpactAnalysisSummary
{
    public required IReadOnlyList<ImpactedPackageResult> Results { get; init; }

    public required string Hint { get; init; }
}
