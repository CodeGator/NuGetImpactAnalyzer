using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class ImpactAnalysisInteractionServiceTests
{
    private sealed class FakeImpactAnalysisService : IImpactAnalysisService
    {
        public required IReadOnlyList<ImpactedPackageResult> NextResults { get; init; }

        public IReadOnlyList<ImpactedPackageResult> AnalyzeImpact(string packageName) => NextResults;
    }

    [Fact]
    public void AnalyzeAndHighlight_CallsImpactAndReturnsSameResults()
    {
        var results = new List<ImpactedPackageResult>
        {
            new()
            {
                NodeId = "r/A",
                DisplayLabel = "A",
                Severity = ImpactSeverity.Definite,
            },
            new()
            {
                NodeId = "r/B",
                DisplayLabel = "B",
                Severity = ImpactSeverity.Possible,
            },
        };
        var impact = new FakeImpactAnalysisService { NextResults = results };
        var sut = new ImpactAnalysisInteractionService(impact);

        var summary = sut.AnalyzeAndHighlight("Lib");

        Assert.Same(results, summary.Results);
    }

    [Fact]
    public void AnalyzeAndHighlight_FormatsHintFromDefiniteAndPossibleCounts()
    {
        var results = new List<ImpactedPackageResult>
        {
            new() { NodeId = "1", DisplayLabel = "X", Severity = ImpactSeverity.Definite },
            new() { NodeId = "2", DisplayLabel = "Y", Severity = ImpactSeverity.Definite },
            new() { NodeId = "3", DisplayLabel = "Z", Severity = ImpactSeverity.Possible },
        };
        var impact = new FakeImpactAnalysisService { NextResults = results };
        var sut = new ImpactAnalysisInteractionService(impact);

        var summary = sut.AnalyzeAndHighlight("t");

        Assert.Equal(ImpactAnalysisPresentation.FormatImpactSummaryHint(2, 1), summary.Hint);
    }

    [Fact]
    public void AnalyzeAndHighlight_WhenNoImpactedPackages_UsesNoMatchesHint()
    {
        var impact = new FakeImpactAnalysisService { NextResults = [] };
        var sut = new ImpactAnalysisInteractionService(impact);

        var summary = sut.AnalyzeAndHighlight("UnknownPackage");

        Assert.Empty(summary.Results);
        Assert.Equal(ImpactAnalysisPresentation.NoMatchesHint, summary.Hint);
    }
}
