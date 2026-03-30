using NuGetImpactAnalyzer.Core;

namespace NuGetImpactAnalyzer.Tests.Core;

public sealed class PackageVersionSatisfactionTests
{
    [Fact]
    public void IsSatisfied_RangeIncludesVersion_ReturnsTrue()
    {
        Assert.True(PackageVersionSatisfaction.IsSatisfied("[1.0, 2.0)", "1.5.0"));
    }

    [Fact]
    public void IsSatisfied_VersionOutsideRange_ReturnsFalse()
    {
        Assert.False(PackageVersionSatisfaction.IsSatisfied("[1.0.0, 1.5.0]", "2.0.0"));
    }

    [Fact]
    public void IsSatisfied_MissingRequired_TreatedAsUnconstrained_ReturnsTrue()
    {
        Assert.True(PackageVersionSatisfaction.IsSatisfied(null, "1.0.0"));
    }

    [Fact]
    public void IsSatisfied_MissingResolved_ReturnsFalse()
    {
        Assert.False(PackageVersionSatisfaction.IsSatisfied("1.0.0", null));
    }

    [Fact]
    public void IsSatisfied_InvalidRangeString_TreatedAsUnconstrained_ReturnsTrue()
    {
        Assert.True(PackageVersionSatisfaction.IsSatisfied("not-a-valid-range", "1.0.0"));
    }

    [Fact]
    public void IsSatisfied_InvalidResolvedVersionString_ReturnsFalse()
    {
        Assert.False(PackageVersionSatisfaction.IsSatisfied("[1.0.0, 2.0.0]", "not-a-version"));
    }
}
