using NuGetImpactAnalyzer.Core;

namespace NuGetImpactAnalyzer.Tests.Core;

public sealed class RepositoryNameFromUrlTests
{
    [Theory]
    [InlineData("https://github.com/CodeGator/MyRepo.git", "MyRepo")]
    [InlineData("https://github.com/org/repo", "repo")]
    [InlineData("HTTPS://GITHUB.COM/X/Y/", "Y")]
    [InlineData("git@github.com:CodeGator/CG.Linq.git", "CG.Linq")]
    [InlineData("ssh://git@github.com/org/project.git", "project")]
    public void Derive_ReturnsLastPathSegment(string url, string expected)
    {
        Assert.Equal(expected, RepositoryNameFromUrl.Derive(url));
    }

    [Fact]
    public void Derive_EmptyOrWhitespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, RepositoryNameFromUrl.Derive(null));
        Assert.Equal(string.Empty, RepositoryNameFromUrl.Derive(""));
        Assert.Equal(string.Empty, RepositoryNameFromUrl.Derive("   "));
    }
}
