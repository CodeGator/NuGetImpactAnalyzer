using NuGetImpactAnalyzer.Core;

namespace NuGetImpactAnalyzer.Tests.Core;

public sealed class ImpactAnalysisPresentationTests
{
    [Fact]
    public void FormatImpactSummaryHint_ZeroZero_UsesNoMatchesCopy()
    {
        Assert.Equal(ImpactAnalysisPresentation.NoMatchesHint, ImpactAnalysisPresentation.FormatImpactSummaryHint(0, 0));
    }

    [Fact]
    public void FormatImpactSummaryHint_NonZero_IncludesDefiniteAndPossible()
    {
        var hint = ImpactAnalysisPresentation.FormatImpactSummaryHint(2, 3);

        Assert.Contains("2", hint, StringComparison.Ordinal);
        Assert.Contains("3", hint, StringComparison.Ordinal);
        Assert.Contains("definite", hint, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("possible", hint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatBuildOrderSuccessHint_IncludesStepCount()
    {
        var hint = ImpactAnalysisPresentation.FormatBuildOrderSuccessHint(5);

        Assert.Contains("5", hint, StringComparison.Ordinal);
        Assert.Contains("step", hint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatNumberedBuildOrderLines_PrefixesIndices()
    {
        var lines = ImpactAnalysisPresentation.FormatNumberedBuildOrderLines(["B", "C", "App"]);

        Assert.Equal(["1. B", "2. C", "3. App"], lines);
    }

    [Fact]
    public void FormatNumberedBuildOrderLines_Empty_YieldsEmptyList()
    {
        Assert.Empty(ImpactAnalysisPresentation.FormatNumberedBuildOrderLines([]));
    }
}
