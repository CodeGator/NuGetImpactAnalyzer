using NuGetImpactAnalyzer.Core;

namespace NuGetImpactAnalyzer.Tests.Core;

public sealed class AppConstantsTests
{
    [Fact]
    public void ApplicationTitle_IsNonEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(AppConstants.ApplicationTitle));
        Assert.Contains("NuGet", AppConstants.ApplicationTitle, StringComparison.OrdinalIgnoreCase);
    }
}
