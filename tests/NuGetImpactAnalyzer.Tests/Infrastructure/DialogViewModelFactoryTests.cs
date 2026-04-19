using System.IO;
using NuGetImpactAnalyzer.Infrastructure;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;
using NuGetImpactAnalyzer.ViewModels;

namespace NuGetImpactAnalyzer.Tests.Infrastructure;

public sealed class DialogViewModelFactoryTests
{
    private sealed class NoOpCredentialsLauncher : IRepositoryCredentialsDialogLauncher
    {
        public void ShowRepositoryCredentialsDialog(IRepositoryCredentialContext context, Action? onClosed = null) =>
            onClosed?.Invoke();
    }

    private sealed class NoOpCredentialService : ICredentialService
    {
        public void DeleteToken(string repoName) { }

        public string? GetToken(string repoName) => null;

        public void SaveToken(string repoName, string token) { }
    }

    private sealed class NoOpGitService : IGitService
    {
        public string GetRepositoriesRoot() => Path.GetTempPath();

        public string GetLocalRepositoryPath(Repo repo) => "";

        public bool IsLocalClonePresent(Repo repo) => false;

        public string? TryGetHeadCommitSha(string localRepositoryPath) => null;

        public Task CloneOrUpdateAsync(Repo repo, IProgress<string>? progress = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SyncAllAsync(IEnumerable<Repo> repos, IProgress<string>? progress = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public IReadOnlyList<string> ListBranches(Repo repo) => [];

        public IReadOnlyList<string> ListProjectRelativePaths(Repo repo) => [];

        public bool TryProbeRemoteRepository(Repo repo) => false;

        public bool TryDeleteLocalClone(Repo repo) => true;
    }

    private sealed class NoOpGitHubMetadata : IGitHubRepositoryMetadataService
    {
        public Task<IReadOnlyList<(Repo Repo, bool? GitHubReportsPrivate)>> GetGitHubPrivateFlagsAsync(
            IReadOnlyList<Repo> repos,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<(Repo Repo, bool? GitHubReportsPrivate)>>([]);

        public Task<GitHubRepositoryUrlValidationResult> ValidateGitHubRepositoryUrlAsync(
            string url,
            string credentialLookupName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new GitHubRepositoryUrlValidationResult(true));
    }

    [Fact]
    public void CreateEditRepositoryViewModel_CopiesRepoFieldsToDraft()
    {
        var repo = new Repo
        {
            Name = "N",
            Url = "https://u.git",
            Branch = "topic",
            AnalysisProjectRelativePath = "src/Entry.csproj",
        };
        var sut = new DialogViewModelFactory(
            new NoOpCredentialsLauncher(),
            new NoOpCredentialService(),
            new NoOpGitService(),
            new NoOpGitHubMetadata());

        var vm = sut.CreateEditRepositoryViewModel(repo, RepositoryEditorDialogKind.Edit);

        Assert.Equal("N", vm.Name);
        Assert.Equal("https://u.git", vm.Url);
        Assert.Equal("topic", vm.Branch);
        Assert.Equal("src/Entry.csproj", vm.AnalysisProjectRelativePath);
    }

    private sealed class FakeCredentialContext : IRepositoryCredentialContext
    {
        public string CredentialKeyName => "TestRepo";

        public event EventHandler? CredentialKeyNameChanged
        {
            add { }
            remove { }
        }
    }

    [Fact]
    public void CreateRepositoryCredentialsViewModel_SubscribesToContext()
    {
        var ctx = new FakeCredentialContext();
        var sut = new DialogViewModelFactory(
            new NoOpCredentialsLauncher(),
            new NoOpCredentialService(),
            new NoOpGitService(),
            new NoOpGitHubMetadata());

        using var vm = sut.CreateRepositoryCredentialsViewModel(ctx);

        Assert.NotNull(vm);
    }
}
