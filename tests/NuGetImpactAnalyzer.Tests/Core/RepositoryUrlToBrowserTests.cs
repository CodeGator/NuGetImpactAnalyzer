using NuGetImpactAnalyzer.Core;

namespace NuGetImpactAnalyzer.Tests.Core;

public sealed class RepositoryUrlToBrowserTests
{
    [Theory]
    [InlineData("https://github.com/org/repo", "https://github.com/org/repo")]
    [InlineData("https://github.com/org/repo.git", "https://github.com/org/repo.git")]
    [InlineData("http://example.com/a/b", "http://example.com/a/b")]
    public void TryGetBrowserUrl_HttpPassthrough(string input, string expected)
    {
        Assert.True(RepositoryUrlToBrowser.TryGetBrowserUrl(input, out var url));
        Assert.Equal(expected, url);
    }

    [Fact]
    public void TryGetBrowserUrl_ScpToHttps()
    {
        Assert.True(RepositoryUrlToBrowser.TryGetBrowserUrl("git@github.com:Org/SomeRepo.git", out var url));
        Assert.Equal("https://github.com/Org/SomeRepo", url);
    }

    [Fact]
    public void TryGetBrowserUrl_SshScheme()
    {
        Assert.True(RepositoryUrlToBrowser.TryGetBrowserUrl("ssh://git@github.com/org/project.git", out var url));
        Assert.Equal("https://github.com/org/project", url);
    }

    [Fact]
    public void TryGetBrowserUrl_Empty_ReturnsFalse()
    {
        Assert.False(RepositoryUrlToBrowser.TryGetBrowserUrl(null, out _));
        Assert.False(RepositoryUrlToBrowser.TryGetBrowserUrl("   ", out _));
    }
}
