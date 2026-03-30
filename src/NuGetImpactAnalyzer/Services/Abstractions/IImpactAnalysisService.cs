using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Computes transitive dependents in the dependency graph (reverse traversal from a target package/project).
/// </summary>
public interface IImpactAnalysisService
{
    /// <summary>
    /// Returns transitive dependents with semantic severity: <see cref="ImpactSeverity.Definite"/> when a path
    /// exists where each package edge's required range satisfies the dependency's resolved version (see NuGet.Versioning).
    /// </summary>
    IReadOnlyList<ImpactedPackageResult> AnalyzeImpact(string packageName);
}
