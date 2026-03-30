using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using NuGetImpactAnalyzer.ViewModels;

namespace NuGetImpactAnalyzer.Views;

public partial class RepositoryCredentialsWindow : Window
{
    private readonly RepositoryCredentialsViewModel _viewModel;
    private bool _syncingPasswordFromViewModel;

    public RepositoryCredentialsWindow(RepositoryCredentialsViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        _viewModel.PropertyChanged += ViewModel_OnPropertyChanged;
        Loaded += (_, _) => SyncPasswordFromViewModel();
        Closed += OnClosed;
    }

    private void ViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RepositoryCredentialsViewModel.PendingToken))
        {
            SyncPasswordFromViewModel();
        }
    }

    private void SyncPasswordFromViewModel()
    {
        var desired = _viewModel.PendingToken ?? string.Empty;
        if (TokenPasswordBox.Password == desired)
        {
            return;
        }

        _syncingPasswordFromViewModel = true;
        try
        {
            TokenPasswordBox.Password = desired;
        }
        finally
        {
            _syncingPasswordFromViewModel = false;
        }
    }

    private void TokenPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_syncingPasswordFromViewModel)
        {
            return;
        }

        if (sender is PasswordBox pb)
        {
            _viewModel.PendingToken = pb.Password;
            _viewModel.SaveTokenCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
        _viewModel.Dispose();
    }
}
