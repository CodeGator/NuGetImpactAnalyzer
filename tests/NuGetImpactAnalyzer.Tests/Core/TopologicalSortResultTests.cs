using NuGetImpactAnalyzer.Core;

namespace NuGetImpactAnalyzer.Tests.Core;

public sealed class TopologicalSortResultTests
{
    [Fact]
    public void Success_CarriesOrderAndNoError()
    {
        var r = new TopologicalSortResult(true, ["a", "b"], null);

        Assert.True(r.Success);
        Assert.Equal(["a", "b"], r.Order);
        Assert.Null(r.ErrorMessage);
    }

    [Fact]
    public void Failure_CarriesMessageAndNoOrder()
    {
        var r = new TopologicalSortResult(false, null, "cycle");

        Assert.False(r.Success);
        Assert.Null(r.Order);
        Assert.Equal("cycle", r.ErrorMessage);
    }
}
