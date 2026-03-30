using NuGetImpactAnalyzer.Core;

namespace NuGetImpactAnalyzer.Tests.Core;

public sealed class RepositoryUrlNormalizerTests
{
    [Fact]
    public void AreSame_HttpsWithOrWithoutGitAndSlash()
    {
        Assert.True(RepositoryUrlNormalizer.AreSame(
            "https://github.com/CodeGator/MyRepo.git",
            "https://github.com/CodeGator/MyRepo/"));
    }

    [Fact]
    public void AreSame_HttpsAndScpGitHub()
    {
        Assert.True(RepositoryUrlNormalizer.AreSame(
            "https://github.com/org/repo.git",
            "git@github.com:org/repo"));
    }

    [Fact]
    public void AreSame_DifferentRepos_ReturnsFalse()
    {
        Assert.False(RepositoryUrlNormalizer.AreSame(
            "https://github.com/a/b",
            "https://github.com/a/c"));
    }
}
