namespace NuGetImpactAnalyzer.Core;

/// <summary>
/// Result of a topological ordering attempt on a vertex set.
/// </summary>
public sealed record TopologicalSortResult(bool Success, IReadOnlyList<string>? Order, string? ErrorMessage);
