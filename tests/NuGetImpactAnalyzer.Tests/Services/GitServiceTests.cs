using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class GitServiceTests
{
    private sealed class NullCredentialService : ICredentialService
    {
        public void DeleteToken(string repoName)
        {
        }

        public string? GetToken(string repoName) => null;

        public void SaveToken(string repoName, string token)
        {
        }
    }

    [Fact]
    public void GetLocalRepositoryPath_CombinesDataRootWithTrimmedRepoName()
    {
        var dataRoot = Path.Combine(Path.GetTempPath(), "nuget-git-data-" + Guid.NewGuid().ToString("N"));
        var sut = new GitService(dataRoot, new NullCredentialService());
        var repo = new Repo { Name = "  MyRepo  ", Url = "https://example.com/x.git", Branch = "main" };

        var path = sut.GetLocalRepositoryPath(repo);

        Assert.Equal(Path.Combine(dataRoot, "MyRepo"), path);
    }

    [Fact]
    public void GetLocalRepositoryPath_WhenNameIsWhitespace_UsesDefaultFolderName()
    {
        var dataRoot = Path.Combine(Path.GetTempPath(), "nuget-git-data-" + Guid.NewGuid().ToString("N"));
        var sut = new GitService(dataRoot, new NullCredentialService());
        var repo = new Repo { Name = "   ", Url = "https://example.com/x.git", Branch = "main" };

        var path = sut.GetLocalRepositoryPath(repo);

        Assert.Equal(Path.Combine(dataRoot, "unnamed-repo"), path);
    }

    [Fact]
    public void GetLocalRepositoryPath_ReplacesInvalidFileNameCharactersWithUnderscore()
    {
        var dataRoot = Path.Combine(Path.GetTempPath(), "nuget-git-data-" + Guid.NewGuid().ToString("N"));
        var sut = new GitService(dataRoot, new NullCredentialService());
        var repo = new Repo { Name = "a<b", Url = "https://example.com/x.git", Branch = "main" };

        var path = sut.GetLocalRepositoryPath(repo);

        Assert.DoesNotContain("<", path);
        Assert.Equal(Path.Combine(dataRoot, "a_b"), path);
    }

    [Fact]
    public void TryGetHeadCommitSha_WhenPathNotARepository_ReturnsNull()
    {
        var dataRoot = Path.Combine(Path.GetTempPath(), "nuget-git-head-" + Guid.NewGuid().ToString("N"));
        var sut = new GitService(dataRoot, new NullCredentialService());

        Assert.Null(sut.TryGetHeadCommitSha(dataRoot));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void TryGetHeadCommitSha_WhenPathNullOrWhitespace_ReturnsNull(string? path)
    {
        var dataRoot = Path.Combine(Path.GetTempPath(), "nuget-git-head-" + Guid.NewGuid().ToString("N"));
        var sut = new GitService(dataRoot, new NullCredentialService());

        Assert.Null(sut.TryGetHeadCommitSha(path!));
    }

    [Fact]
    public void TryDeleteLocalClone_WhenCloneMissing_ReturnsTrue()
    {
        var dataRoot = Path.Combine(Path.GetTempPath(), "nuget-git-del-" + Guid.NewGuid().ToString("N"));
        var sut = new GitService(dataRoot, new NullCredentialService());
        var repo = new Repo { Name = "Ghost", Url = "https://example.com/x.git", Branch = "main" };

        Assert.True(sut.TryDeleteLocalClone(repo));
        Assert.False(Directory.Exists(sut.GetLocalRepositoryPath(repo)));
    }

    [Fact]
    public void TryDeleteLocalClone_RemovesDirectoryAndContents()
    {
        var dataRoot = Path.Combine(Path.GetTempPath(), "nuget-git-del-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataRoot);
        var sut = new GitService(dataRoot, new NullCredentialService());
        var repo = new Repo { Name = "R1", Url = "https://example.com/x.git", Branch = "main" };
        var clone = sut.GetLocalRepositoryPath(repo);
        Directory.CreateDirectory(clone);
        var nested = Path.Combine(clone, "sub");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "file.txt"), "x");
        File.SetAttributes(Path.Combine(nested, "file.txt"), FileAttributes.ReadOnly);

        Assert.True(sut.TryDeleteLocalClone(repo));
        Assert.False(Directory.Exists(clone));
    }
}
