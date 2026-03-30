using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Tests.Core;

public sealed class CatalogLogFormatterTests
{
    [Fact]
    public void Format_IncludesRepoCountAndNoWarningLineWhenWarningNull()
    {
        var result = new ConfigurationLoadResult(
            new AppConfig
            {
                Repos =
                [
                    new Repo { Name = "A", Url = "u", Branch = "main" },
                    new Repo { Name = "B", Url = "u", Branch = "main" },
                ],
            },
            Warning: null);

        var lines = CatalogLogFormatter.Format(result).ToList();

        Assert.Single(lines);
        Assert.Contains("Loaded 2 repo(s) from config.", lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Format_WhenWarningPresent_YieldsSecondLineWithWarning()
    {
        var result = new ConfigurationLoadResult(new AppConfig(), Warning: "config missing");

        var lines = CatalogLogFormatter.Format(result).ToList();

        Assert.Equal(2, lines.Count);
        Assert.Contains("Loaded 0 repo(s)", lines[0], StringComparison.Ordinal);
        Assert.Contains("config missing", lines[1], StringComparison.Ordinal);
    }
}
