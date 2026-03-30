namespace NuGetImpactAnalyzer.Models;

/// <summary>
/// Semantic impact classification for transitive dependents (version constraints vs resolved versions).
/// </summary>
public enum ImpactSeverity
{
    /// <summary>No transitive impact from the current analysis.</summary>
    None = 0,

    /// <summary>Every package edge on some path to the target satisfies the required range vs resolved version.</summary>
    Definite = 1,

    /// <summary>Impacted in the graph, but version data is missing or constraints are not satisfied on every path.</summary>
    Possible = 2,
}
