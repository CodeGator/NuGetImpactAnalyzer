namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Result of computing a topological build order on the impacted subgraph.
/// </summary>
public sealed record BuildOrderResult(bool Success, IReadOnlyList<string> OrderedPackages, string? ErrorMessage);
