using NuGetImpactAnalyzer.Services;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class GitHubRepoUrlParserTests
{
    [Theory]
    [InlineData("https://github.com/org/repo.git", "org", "repo")]
    [InlineData("https://github.com/org/repo", "org", "repo")]
    [InlineData("HTTPS://GITHUB.COM/Org/Repo.GIT", "Org", "Repo")]
    [InlineData("http://github.com/a/b.git", "a", "b")]
    [InlineData("git@github.com:org/slug.git", "org", "slug")]
    [InlineData("git@github.com:my-org/my.repo.git", "my-org", "my.repo")]
    [InlineData("ssh://git@github.com/org/repo.git", "org", "repo")]
    public void TryParseGitHubRepository_ValidUrls_ReturnsOwnerAndRepo(string url, string expectedOwner, string expectedRepo)
    {
        var ok = GitHubRepoUrlParser.TryParseGitHubRepository(url, out var owner, out var repo);

        Assert.True(ok);
        Assert.Equal(expectedOwner, owner);
        Assert.Equal(expectedRepo, repo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("https://gitlab.com/g/p.git")]
    [InlineData("https://github.com/onlyone")]
    [InlineData("file:///C:/temp/repo")]
    public void TryParseGitHubRepository_NotGitHubOrInvalid_ReturnsFalse(string? url)
    {
        var ok = GitHubRepoUrlParser.TryParseGitHubRepository(url, out _, out _);

        Assert.False(ok);
    }
}
