using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Infrastructure;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// Draft values for editing a <see cref="Repo"/> in a modal dialog.
/// </summary>
public sealed partial class EditRepositoryViewModel : ObservableObject, IRepositoryCredentialContext
{
    private readonly Repo _repo;
    private readonly RepositoryEditorDialogKind _kind;
    private readonly IRepositoryCredentialsDialogLauncher _credentialsDialogs;
    private readonly IGitService _gitService;
    private readonly IGitHubRepositoryMetadataService _gitHubMetadata;
    private readonly Action? _onCredentialsDialogClosed;
    private readonly Func<string, bool>? _isRepositoryUrlAlreadyUsed;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _branch = string.Empty;

    [ObservableProperty]
    private string _analysisProjectRelativePath = string.Empty;

    [ObservableProperty]
    private bool _isRefreshingLists;

    public ObservableCollection<string> BranchOptions { get; } = [];

    public ObservableCollection<ProjectScopeOption> ProjectScopeOptions { get; } = [];

    public EditRepositoryViewModel(
        Repo repo,
        IRepositoryCredentialsDialogLauncher credentialsDialogs,
        IGitService gitService,
        IGitHubRepositoryMetadataService gitHubMetadata,
        RepositoryEditorDialogKind kind = RepositoryEditorDialogKind.Edit,
        Action? onCredentialsDialogClosed = null,
        Func<string, bool>? isRepositoryUrlAlreadyUsed = null)
    {
        _repo = repo;
        _kind = kind;
        _credentialsDialogs = credentialsDialogs;
        _gitService = gitService;
        _gitHubMetadata = gitHubMetadata;
        _onCredentialsDialogClosed = onCredentialsDialogClosed;
        _isRepositoryUrlAlreadyUsed = isRepositoryUrlAlreadyUsed;
        _url = repo.Url ?? string.Empty;
        _branch = repo.Branch ?? string.Empty;
        _analysisProjectRelativePath = repo.AnalysisProjectRelativePath ?? string.Empty;
        DialogTitle = kind == RepositoryEditorDialogKind.Add
            ? "Add repository"
            : "Edit repository";

        if (kind == RepositoryEditorDialogKind.Add)
        {
            _name = string.IsNullOrWhiteSpace(_url)
                ? string.Empty
                : RepositoryNameFromUrl.Derive(_url);
        }
        else
        {
            _name = repo.Name ?? string.Empty;
        }

        _ = RefreshListsAsync();
    }

    /// <summary>Caption for the host window.</summary>
    public string DialogTitle { get; }

    /// <summary>Add mode: name is derived from URL and read-only. Edit mode: user can rename.</summary>
    public bool IsNameReadOnly => _kind == RepositoryEditorDialogKind.Add;

    /// <summary>
    /// Requests closing the host window; the view assigns <see cref="System.Windows.Window.DialogResult"/>.
    /// </summary>
    public event EventHandler<DialogResultRequestedEventArgs>? CloseRequested;

    /// <inheritdoc />
    public string CredentialKeyName => Name;

    /// <inheritdoc />
    public event EventHandler? CredentialKeyNameChanged;

    partial void OnNameChanged(string value)
    {
        CredentialKeyNameChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnUrlChanged(string value)
    {
        if (_kind == RepositoryEditorDialogKind.Add)
        {
            Name = RepositoryNameFromUrl.Derive(value);
        }
    }

    [RelayCommand]
    private void ManageCredentials() =>
        _credentialsDialogs.ShowRepositoryCredentialsDialog(this, _onCredentialsDialogClosed);

    [RelayCommand]
    private async Task RefreshListsAsync()
    {
        if (IsRefreshingLists)
        {
            return;
        }

        IsRefreshingLists = true;
        try
        {
            var draft = CreateDraftRepoForGitQueries();
            List<string> branches;
            List<string> projectPaths;
            try
            {
                branches = await Task.Run(() => _gitService.ListBranches(draft).ToList()).ConfigureAwait(true);
                projectPaths = await Task.Run(() => _gitService.ListProjectRelativePaths(draft).ToList())
                    .ConfigureAwait(true);
            }
            catch
            {
                branches = [];
                projectPaths = [];
            }

            PostToUi(() =>
            {
                RebuildBranchOptions(branches);
                RebuildProjectOptions(projectPaths);
            });
        }
        finally
        {
            PostToUi(() => IsRefreshingLists = false);
        }
    }

    [RelayCommand]
    private async Task Confirm()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            CenteredMessageBox.Show(
                DialogOwnerWindow.Resolve(),
                "Enter a repository name.",
                "Repository name",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var trimmedUrl = (Url ?? string.Empty).Trim();
        if (_isRepositoryUrlAlreadyUsed?.Invoke(trimmedUrl) == true)
        {
            CenteredMessageBox.Show(
                DialogOwnerWindow.Resolve(),
                "Another repository in the list already uses this URL (or an equivalent HTTPS/SSH form).",
                "Duplicate repository URL",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var validation = await _gitHubMetadata
            .ValidateGitHubRepositoryUrlAsync(trimmedUrl, Name.Trim(), CancellationToken.None)
            .ConfigureAwait(true);
        if (!validation.IsValid)
        {
            // REST API can return 403 (e.g. anonymous rate limit) while git ls-remote still works — same path as Refresh.
            var gitReachable = GitHubRepoUrlParser.TryParseGitHubRepository(trimmedUrl, out _, out _)
                && await Task.Run(() => _gitService.TryProbeRemoteRepository(CreateDraftRepoForGitQueries()))
                    .ConfigureAwait(true);
            if (!gitReachable)
            {
                CenteredMessageBox.Show(
                    DialogOwnerWindow.Resolve(),
                    validation.ErrorMessage ?? "The repository URL could not be verified.",
                    "GitHub repository",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
        }

        ApplyToModel();
        CloseRequested?.Invoke(this, new DialogResultRequestedEventArgs(true));
    }

    private Repo CreateDraftRepoForGitQueries() =>
        new()
        {
            Name = Name,
            Url = Url,
            Branch = string.IsNullOrWhiteSpace(Branch) ? "main" : Branch.Trim(),
        };

    private void RebuildBranchOptions(List<string> branches)
    {
        BranchOptions.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var b in branches.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
        {
            if (seen.Add(b))
            {
                BranchOptions.Add(b);
            }
        }

        var effective = string.IsNullOrWhiteSpace(Branch) ? "main" : Branch.Trim();
        if (!string.IsNullOrEmpty(effective) && seen.Add(effective))
        {
            BranchOptions.Insert(0, effective);
        }
    }

    private void RebuildProjectOptions(List<string> projectPaths)
    {
        ProjectScopeOptions.Clear();
        ProjectScopeOptions.Add(new ProjectScopeOption("", "All projects (entire repository)"));
        foreach (var p in projectPaths)
        {
            ProjectScopeOptions.Add(new ProjectScopeOption(p, p));
        }

        var scope = (AnalysisProjectRelativePath ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(scope))
        {
            return;
        }

        var known = projectPaths.Any(p => p.Equals(scope, StringComparison.OrdinalIgnoreCase));
        if (!known)
        {
            ProjectScopeOptions.Add(new ProjectScopeOption(scope, scope + " (saved path)"));
        }
    }

    private void ApplyToModel()
    {
        _repo.Name = Name;
        _repo.Url = Url;
        _repo.Branch = Branch;
        _repo.AnalysisProjectRelativePath = (AnalysisProjectRelativePath ?? string.Empty).Trim();
    }

    private static void PostToUi(Action action)
    {
        var d = Application.Current?.Dispatcher;
        if (d is null || d.CheckAccess())
        {
            action();
        }
        else
        {
            d.Invoke(action);
        }
    }

    /// <summary>Entry for the analysis-root project dropdown (<see cref="ProjectScopeOption.Path"/> is persisted).</summary>
    public sealed class ProjectScopeOption
    {
        public ProjectScopeOption(string path, string caption)
        {
            Path = path;
            Caption = caption;
        }

        public string Path { get; }
        public string Caption { get; }
    }
}
