using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Infrastructure;
using NuGetImpactAnalyzer.Services.Abstractions;
using NuGetImpactAnalyzer.Views;

namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// Application shell: window chrome, shared output log, and composed panel view models.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    public RepositoryWorkspaceViewModel Workspace { get; }

    public RepositorySyncViewModel Sync { get; }

    public DependencyGraphViewModel Graph { get; }

    public ImpactAnalysisViewModel Impact { get; }

    public ApplicationLogViewModel Log { get; }

    public StatusBarViewModel StatusBar { get; }

    private readonly IMasterPasswordService _masterPassword;
    private readonly IApplicationSessionController _session;

    [ObservableProperty]
    private string _windowTitle = AppConstants.ApplicationTitle;

    public MainViewModel(
        RepositoryWorkspaceViewModel workspace,
        RepositorySyncViewModel sync,
        DependencyGraphViewModel graph,
        ImpactAnalysisViewModel impact,
        ApplicationLogViewModel log,
        StatusBarViewModel statusBar,
        IMasterPasswordService masterPassword,
        IApplicationSessionController session)
    {
        Workspace = workspace;
        Sync = sync;
        Graph = graph;
        Impact = impact;
        Log = log;
        StatusBar = statusBar;
        _masterPassword = masterPassword;
        _session = session;
    }

    [RelayCommand]
    private void ClearOutput() => Log.Clear();

    [RelayCommand]
    private void ChangeMasterPassword()
    {
        var vm = new ChangeMasterPasswordViewModel(
            _masterPassword,
            () => Workspace.Repositories.Select(r => r.Name).ToList());
        var dialog = new ChangeMasterPasswordWindow(vm)
        {
            Owner = DialogOwnerWindow.Resolve(),
        };
        dialog.ShowDialog();
    }

    [RelayCommand]
    private void Exit() => System.Windows.Application.Current.Shutdown(0);

    [RelayCommand]
    private void About()
    {
        var owner = DialogOwnerWindow.Resolve();
        var dialog = new AboutWindow { Owner = owner };
        dialog.ShowDialog();
    }

    [RelayCommand]
    private void Logout()
    {
        _masterPassword.Lock();
        _session.RequestLogout();
    }
}
