using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Orchestrates impact analysis and graph highlighting so the Impact view model does not coordinate multiple services.
/// </summary>
public interface IImpactAnalysisInteractionService
{
    /// <summary>
    /// Runs semantic impact analysis and updates the dependency graph visualization highlights.
    /// </summary>
    ImpactAnalysisSummary AnalyzeAndHighlight(string targetPackage);
}
