using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuGetImpactAnalyzer.Infrastructure;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// Git sync workflow for configured repositories: progress UI and status reporting.
/// </summary>
public partial class RepositorySyncViewModel : ObservableObject
{
    private readonly IRepositorySyncCoordinator _syncCoordinator;
    private readonly IGitService _git;
    private readonly RepositoryWorkspaceViewModel _workspace;
    private readonly IApplicationLog _log;
    private readonly IClock _clock;
    private readonly IApplicationStatus _status;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SyncReposCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearClonesCommand))]
    private bool _syncUiEnabled = true;

    [ObservableProperty]
    private bool _isSyncingRepositories;

    [ObservableProperty]
    private bool _isClearingClones;

    public RepositorySyncViewModel(
        IRepositorySyncCoordinator syncCoordinator,
        IGitService git,
        RepositoryWorkspaceViewModel workspace,
        IApplicationLog log,
        IClock clock,
        IApplicationStatus status)
    {
        _syncCoordinator = syncCoordinator;
        _git = git;
        _workspace = workspace;
        _log = log;
        _clock = clock;
        _status = status;
    }

    [RelayCommand(CanExecute = nameof(CanSyncRepos))]
    private async Task SyncReposAsync()
    {
        SyncUiEnabled = false;
        IsSyncingRepositories = true;
        _status.SetBusy("Syncing repositories…");
        try
        {
            await _syncCoordinator.SyncAllAsync(_workspace.Repositories.ToList(), _log, CancellationToken.None)
                .ConfigureAwait(true);
            await _workspace.RefreshGitHubRepositoryMetadataAsync().ConfigureAwait(true);

            _status.SetReady("Sync finished.");
        }
        catch (OperationCanceledException)
        {
            _log.AppendTimestampedLine(_clock, "Sync cancelled.");
            _status.SetReady("Sync cancelled.");
        }
        catch (Exception ex)
        {
            _log.AppendTimestampedLine(_clock, $"Sync failed: {ex.Message}");
            _status.SetError($"Sync failed: {ex.Message}");
        }
        finally
        {
            IsSyncingRepositories = false;
            SyncUiEnabled = true;
        }
    }

    private bool CanSyncRepos() => SyncUiEnabled;

    [RelayCommand(CanExecute = nameof(CanClearClones))]
    private async Task ClearClonesAsync()
    {
        if (_workspace.Repositories.Count == 0)
        {
            _log.AppendTimestampedLine(_clock, "No repositories configured; nothing to clear.");
            return;
        }

        var owner = DialogOwnerWindow.Resolve();
        var n = _workspace.Repositories.Count;
        var answer = CenteredMessageBox.Show(
            owner,
            $"This removes all locally cloned repository folders under Data for the {n} configured repo(s). "
            + "You can use Sync repos to clone again. Continue?",
            "Clear local clones",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        SyncUiEnabled = false;
        IsClearingClones = true;
        _status.SetBusy("Clearing local clones…");
        var repos = _workspace.Repositories.ToList();
        try
        {
            var failures = await Task.Run(() =>
            {
                List<(string Name, string Path)> list = [];
                foreach (var repo in repos)
                {
                    var path = _git.GetLocalRepositoryPath(repo);
                    if (!_git.TryDeleteLocalClone(repo) && Directory.Exists(path))
                    {
                        list.Add((repo.Name, path));
                    }
                }

                return list;
            }).ConfigureAwait(true);

            foreach (var (name, path) in failures)
            {
                _log.AppendTimestampedLine(
                    _clock,
                    $"Could not delete local clone for '{name}'. Remove the folder manually if needed: {path}");
            }

            if (failures.Count == 0)
            {
                _log.AppendTimestampedLine(_clock, $"Cleared local clones for {repos.Count} repo(s).");
            }

            _status.SetReady(
                failures.Count > 0 ? "Finished with warnings (see output)." : "Local clones cleared.");
        }
        catch (Exception ex)
        {
            _log.AppendTimestampedLine(_clock, $"Clear clones failed: {ex.Message}");
            _status.SetError($"Clear clones failed: {ex.Message}");
        }
        finally
        {
            IsClearingClones = false;
            SyncUiEnabled = true;
        }
    }

    private bool CanClearClones() => SyncUiEnabled;
}
