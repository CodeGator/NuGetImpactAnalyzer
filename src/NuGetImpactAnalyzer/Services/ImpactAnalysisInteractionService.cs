using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

/// <inheritdoc />
public sealed class ImpactAnalysisInteractionService : IImpactAnalysisInteractionService
{
    private readonly IImpactAnalysisService _impactAnalysis;

    public ImpactAnalysisInteractionService(
        IImpactAnalysisService impactAnalysis)
    {
        _impactAnalysis = impactAnalysis;
    }

    /// <inheritdoc />
    public ImpactAnalysisSummary AnalyzeAndHighlight(string targetPackage)
    {
        var list = _impactAnalysis.AnalyzeImpact(targetPackage);

        var definite = list.Count(r => r.Severity == ImpactSeverity.Definite);
        var possible = list.Count(r => r.Severity == ImpactSeverity.Possible);
        var hint = ImpactAnalysisPresentation.FormatImpactSummaryHint(definite, possible);

        return new ImpactAnalysisSummary
        {
            Results = list,
            Hint = hint,
        };
    }
}
