using NuGetImpactAnalyzer.Core;

namespace NuGetImpactAnalyzer.Tests.Core;

public sealed class DependencyGraphPresentationTests
{
    [Fact]
    public void InitialHint_IsNonEmptyAndMentionsBuildGraph()
    {
        Assert.False(string.IsNullOrWhiteSpace(DependencyGraphPresentation.InitialHint));
        Assert.Contains("Build Graph", DependencyGraphPresentation.InitialHint, StringComparison.Ordinal);
    }
}
