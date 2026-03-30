using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.ViewModels;

public sealed partial class ChangeMasterPasswordViewModel : ObservableObject
{
    private readonly IMasterPasswordService _master;
    private readonly Func<IReadOnlyList<string>> _repoNames;

    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public ChangeMasterPasswordViewModel(
        IMasterPasswordService master,
        Func<IReadOnlyList<string>> repoNames)
    {
        _master = master;
        _repoNames = repoNames;
    }

    [RelayCommand]
    private void Save(Window? window)
    {
        if (window is null)
        {
            return;
        }

        ErrorMessage = null;
        if (!_master.TryChangeMasterPassword(
                CurrentPassword,
                NewPassword,
                ConfirmPassword,
                _repoNames(),
                out var err))
        {
            ErrorMessage = err;
            return;
        }

        window.DialogResult = true;
        window.Close();
    }

    [RelayCommand]
    private void Cancel(Window? window)
    {
        if (window is null)
        {
            return;
        }

        window.DialogResult = false;
        window.Close();
    }
}
