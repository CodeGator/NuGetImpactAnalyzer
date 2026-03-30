using NuGetImpactAnalyzer.Core;

namespace NuGetImpactAnalyzer.Tests.Core;

public sealed class DependencyGraphProjectFilterTests
{
    [Theory]
    [InlineData("CG.Alerts.UnitTests")]
    [InlineData("MyApp.Tests")]
    [InlineData("TestApp")]
    [InlineData("MYTEST")]
    [InlineData("X.test.helpers")]
    public void ShouldExclude_WhenNameContainsTest_ReturnsTrue(string name)
    {
        Assert.True(DependencyGraphProjectFilter.ShouldExclude(name));
    }

    [Theory]
    [InlineData("CG.Alerts")]
    [InlineData("MyApp.Core")]
    [InlineData("")]
    [InlineData("   ")]
    public void ShouldExclude_WhenNameHasNoTestSubstring_ReturnsFalse(string? name)
    {
        Assert.False(DependencyGraphProjectFilter.ShouldExclude(name));
    }
}
