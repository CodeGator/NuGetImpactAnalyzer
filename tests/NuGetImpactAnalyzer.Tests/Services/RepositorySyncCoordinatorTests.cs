using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;
using NuGetImpactAnalyzer.Tests.Infrastructure;
using NuGetImpactAnalyzer.ViewModels;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class RepositorySyncCoordinatorTests
{
    private sealed class FakeGitService : IGitService
    {
        public IReadOnlyList<Repo>? CapturedRepos { get; private set; }
        public IProgress<string>? CapturedProgress { get; private set; }
        public CancellationToken? CapturedCancellationToken { get; private set; }
        public Exception? ThrowOnSync { get; init; }

        public string GetLocalRepositoryPath(Repo repo) =>
            Path.Combine(Path.GetTempPath(), "fake-git", repo.Name);

        public Task CloneOrUpdateAsync(Repo repo, IProgress<string>? progress = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SyncAllAsync(IEnumerable<Repo> repos, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            CapturedCancellationToken = cancellationToken;
            if (ThrowOnSync is not null)
            {
                throw ThrowOnSync;
            }

            CapturedRepos = repos.ToList();
            CapturedProgress = progress;
            progress?.Report("[progress] fake git line");
            return Task.CompletedTask;
        }

        public string? TryGetHeadCommitSha(string localRepositoryPath) => null;

        public IReadOnlyList<string> ListBranches(Repo repo) => [];

        public IReadOnlyList<string> ListProjectRelativePaths(Repo repo) => [];

        public bool TryProbeRemoteRepository(Repo repo) => false;

        public bool TryDeleteLocalClone(Repo repo) => true;
    }

    [Fact]
    public async Task SyncAllAsync_CallsGitWithSameReposAndAppendsLogLines()
    {
        var git = new FakeGitService();
        var sut = new RepositorySyncCoordinator(git);
        var log = new ApplicationLogViewModel();
        var repos = new List<Repo>
        {
            new() { Name = "R1", Url = "https://a.git", Branch = "main" },
        };

        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new ImmediateSynchronizationContext());
        try
        {
            await sut.SyncAllAsync(repos, log);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Assert.NotNull(git.CapturedRepos);
        Assert.Single(git.CapturedRepos!);
        Assert.Equal("R1", git.CapturedRepos![0].Name);
        Assert.Contains("Starting sync for 1 repo", log.Text, StringComparison.Ordinal);
        Assert.Contains("[progress] fake git line", log.Text, StringComparison.Ordinal);
        Assert.Contains("Sync batch finished.", log.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SyncAllAsync_WithEmptyRepoList_StillLogsStartAndFinish()
    {
        var git = new FakeGitService();
        var sut = new RepositorySyncCoordinator(git);
        var log = new ApplicationLogViewModel();

        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new ImmediateSynchronizationContext());
        try
        {
            await sut.SyncAllAsync([], log);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Assert.Contains("Starting sync for 0 repo", log.Text, StringComparison.Ordinal);
        Assert.Contains("Sync batch finished.", log.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SyncAllAsync_WhenGitThrowsOperationCanceledException_Propagates()
    {
        var git = new FakeGitService { ThrowOnSync = new OperationCanceledException() };
        var sut = new RepositorySyncCoordinator(git);
        var log = new ApplicationLogViewModel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            sut.SyncAllAsync([], log, CancellationToken.None));
    }

    [Fact]
    public async Task SyncAllAsync_PassesCancellationTokenToGitService()
    {
        var git = new FakeGitService();
        var sut = new RepositorySyncCoordinator(git);
        var log = new ApplicationLogViewModel();
        using var cts = new CancellationTokenSource();

        await sut.SyncAllAsync([], log, cts.Token);

        Assert.True(git.CapturedCancellationToken.HasValue);
        Assert.Equal(cts.Token, git.CapturedCancellationToken!.Value);
    }
}
