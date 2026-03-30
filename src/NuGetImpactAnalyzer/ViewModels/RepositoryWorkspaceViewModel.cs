using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// Repository catalog and selection for analysis scope (per-repo settings) and UI binding.
/// </summary>
public partial class RepositoryWorkspaceViewModel : ObservableObject
{
    private readonly IRepositoryCatalogService _catalog;
    private readonly IApplicationLog _log;
    private readonly IDialogService _dialogs;
    private readonly IRepositoryListConfigurationSynchronizer _configurationSynchronizer;
    private readonly IGitHubRepositoryMetadataService _gitHubMetadata;
    private readonly ICredentialService _credentials;
    private readonly IGitService _git;
    private readonly IParseResultCache _parseCache;
    private readonly IAnalysisResetService _reset;
    private readonly ICollectionView _repositoriesView;
    private readonly HashSet<Repo> _filterListenerRepos = [];

    public ObservableCollection<Repo> Repositories { get; } = new();

    /// <summary>Filtered view of <see cref="Repositories"/> for the repository grid (search + DataGrid sort).</summary>
    public ICollectionView RepositoriesView => _repositoriesView;

    [ObservableProperty]
    private Repo? _selectedRepository;

    /// <summary>Filters <see cref="Repositories"/> in the UI (name, branch, URL; case-insensitive).</summary>
    [ObservableProperty]
    private string _repositorySearchText = string.Empty;

    public RepositoryWorkspaceViewModel(
        IRepositoryCatalogService catalog,
        IApplicationLog log,
        IDialogService dialogs,
        IRepositoryListConfigurationSynchronizer configurationSynchronizer,
        IGitHubRepositoryMetadataService gitHubMetadata,
        ICredentialService credentials,
        IGitService git,
        IParseResultCache parseCache,
        IAnalysisResetService reset)
    {
        _catalog = catalog;
        _log = log;
        _dialogs = dialogs;
        _configurationSynchronizer = configurationSynchronizer;
        _gitHubMetadata = gitHubMetadata;
        _credentials = credentials;
        _git = git;
        _parseCache = parseCache;
        _reset = reset;
        _configurationSynchronizer.Bind(Repositories);
        _repositoriesView = CollectionViewSource.GetDefaultView(Repositories);
        _repositoriesView.Filter = FilterRepository;

        Repositories.CollectionChanged += OnRepositoriesCollectionChanged;
        foreach (var r in Repositories)
        {
            AttachFilterListener(r);
        }

        RefreshCatalog();
        RefreshStoredCredentialFlags();
        EnsureSelectedRepositoryInView();
        _ = RefreshGitHubRepositoryMetadataAsync();
    }

    partial void OnRepositorySearchTextChanged(string value)
    {
        _repositoriesView.Refresh();
        EnsureSelectedRepositoryInView();
    }

    [RelayCommand]
    private void AddRepository()
    {
        var draft = new Repo();
        var confirmed = _dialogs.ShowEditRepositoryDialog(
            draft,
            RepositoryEditorDialogKind.Add,
            RefreshStoredCredentialFlags,
            url => IsRepositoryUrlUsedByAnother(url, except: null));
        if (!confirmed)
        {
            RefreshStoredCredentialFlags();
            return;
        }

        Repositories.Add(draft);
        SelectedRepository = draft;
        RefreshStoredCredentialFlags();
        _ = RefreshGitHubRepositoryMetadataAsync();
    }

    [RelayCommand(CanExecute = nameof(CanRemoveRepository))]
    private void RemoveRepository(Repo? repo)
    {
        if (repo is null || !Repositories.Contains(repo))
        {
            return;
        }

        var idx = Repositories.IndexOf(repo);
        var wasSelected = SelectedRepository == repo;
        var localPath = _git.GetLocalRepositoryPath(repo);

        _parseCache.RemoveEntriesForRepository(repo.Name);
        if (!_git.TryDeleteLocalClone(repo))
        {
            _log.AppendLine(
                $"Could not delete local clone. Remove the folder manually if it is no longer needed: {localPath}");
        }

        Repositories.Remove(repo);

        if (wasSelected)
        {
            if (Repositories.Count == 0)
            {
                SelectedRepository = null;
            }
            else if (idx >= Repositories.Count)
            {
                SelectedRepository = Repositories[^1];
            }
            else
            {
                SelectedRepository = Repositories[idx];
            }
        }
    }

    private bool CanRemoveRepository(Repo? repo) =>
        repo is not null && Repositories.Contains(repo);

    /// <returns>True if another repository (not <paramref name="except"/>) already has this URL in an equivalent form.</returns>
    private bool IsRepositoryUrlUsedByAnother(string url, Repo? except) =>
        Repositories.Any(r =>
            !ReferenceEquals(r, except) && RepositoryUrlNormalizer.AreSame(r.Url, url));

    [RelayCommand]
    private void OpenRepositoryInBrowser(Repo? repo)
    {
        if (repo is null)
        {
            return;
        }

        if (!RepositoryUrlToBrowser.TryGetBrowserUrl(repo.Url, out var url))
        {
            _log.AppendLine($"Could not derive a web URL to open for '{repo.Name}'.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log.AppendLine($"Could not open browser: {ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanEditRepository))]
    private void EditRepository(Repo? repo)
    {
        if (repo is null)
        {
            return;
        }

        _dialogs.ShowEditRepositoryDialog(
            repo,
            RepositoryEditorDialogKind.Edit,
            RefreshStoredCredentialFlags,
            url => IsRepositoryUrlUsedByAnother(url, repo));
        RefreshStoredCredentialFlags();
        EnsureSelectedRepositoryInView();
        _ = RefreshGitHubRepositoryMetadataAsync();
    }

    private static bool CanEditRepository(Repo? repo) => repo is not null;

    private void RefreshCatalog()
    {
        _configurationSynchronizer.ExecuteWhileSilent(() =>
        {
            var result = _catalog.Refresh(Repositories);

            foreach (var line in CatalogLogFormatter.Format(result))
            {
                _log.AppendLine(line);
            }
        });
    }

    private void RefreshStoredCredentialFlags()
    {
        foreach (var r in Repositories)
        {
            var key = r.Name.Trim();
            r.HasStoredCredentials = !string.IsNullOrWhiteSpace(key)
                && !string.IsNullOrEmpty(_credentials.GetToken(key));
        }
    }

    /// <summary>Queries GitHub for each repo URL and updates <see cref="Repo.GitHubIsPrivate"/>.</summary>
    public async Task RefreshGitHubRepositoryMetadataAsync()
    {
        IReadOnlyList<Repo> snapshot;
        try
        {
            snapshot = Repositories.ToList();
        }
        catch
        {
            return;
        }

        if (snapshot.Count == 0)
        {
            return;
        }

        try
        {
            var results = await _gitHubMetadata.GetGitHubPrivateFlagsAsync(snapshot, CancellationToken.None)
                .ConfigureAwait(true);
            foreach (var (repo, gitHubReportsPrivate) in results)
            {
                if (Repositories.Contains(repo))
                {
                    repo.GitHubIsPrivate = gitHubReportsPrivate;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            _log.AppendLine($"GitHub repository metadata: {ex.Message}");
        }
        finally
        {
            RefreshStoredCredentialFlags();
        }
    }

    private void OnRepositoriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Any change to the repository list invalidates the built graph and analyzer output.
        _reset.RequestReset();

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var r in _filterListenerRepos.ToArray())
            {
                DetachFilterListener(r);
            }

            _filterListenerRepos.Clear();
            foreach (var r in Repositories)
            {
                AttachFilterListener(r);
            }

            return;
        }

        if (e.OldItems is not null)
        {
            foreach (Repo r in e.OldItems)
            {
                DetachFilterListener(r);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (Repo r in e.NewItems)
            {
                AttachFilterListener(r);
            }

            if (SelectedRepository is null && Repositories.Count > 0)
            {
                EnsureSelectedRepositoryInView();
            }
        }
    }

    private void AttachFilterListener(Repo repo)
    {
        if (!_filterListenerRepos.Add(repo))
        {
            return;
        }

        repo.PropertyChanged += OnRepoPropertyChangedForFilter;
    }

    private void DetachFilterListener(Repo repo)
    {
        if (!_filterListenerRepos.Remove(repo))
        {
            return;
        }

        repo.PropertyChanged -= OnRepoPropertyChangedForFilter;
    }

    private void OnRepoPropertyChangedForFilter(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null
            || e.PropertyName is nameof(Repo.Name)
            or nameof(Repo.Branch)
            or nameof(Repo.Url)
            or nameof(Repo.AnalysisProjectRelativePath))
        {
            _reset.RequestReset();
            _repositoriesView.Refresh();
            EnsureSelectedRepositoryInView();
        }
    }

    private bool FilterRepository(object obj)
    {
        if (obj is not Repo r)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(RepositorySearchText))
        {
            return true;
        }

        var q = RepositorySearchText.Trim();
        return r.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
               || r.Branch.Contains(q, StringComparison.OrdinalIgnoreCase)
               || r.Url.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureSelectedRepositoryInView()
    {
        _repositoriesView.Refresh();

        if (SelectedRepository is not null && !Repositories.Contains(SelectedRepository))
        {
            SelectedRepository = null;
        }

        if (SelectedRepository is not null && !FilterRepository(SelectedRepository))
        {
            SelectedRepository = _repositoriesView.OfType<Repo>().FirstOrDefault();
        }

        if (SelectedRepository is null && Repositories.Count > 0)
        {
            SelectedRepository = _repositoriesView.OfType<Repo>().FirstOrDefault() ?? Repositories[0];
        }
    }
}
