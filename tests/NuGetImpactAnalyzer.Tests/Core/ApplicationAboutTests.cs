using NuGetImpactAnalyzer.Core;

namespace NuGetImpactAnalyzer.Tests.Core;

public sealed class ApplicationAboutTests
{
    [Fact]
    public void FormatAboutMessage_IncludesTitleVersionCopyrightRangeAndReserved()
    {
        var text = ApplicationAbout.FormatAboutMessage();

        Assert.Contains(AppConstants.ApplicationTitle, text);
        Assert.Contains("Version ", text);
        Assert.Contains($"Copyright {ApplicationAbout.CopyrightStartYear} - ", text);
        Assert.Contains($"{DateTime.Now.Year} by CodeGator.", text);
        Assert.Contains("All rights reserved.", text);
    }
}
