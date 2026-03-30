using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly IMasterPasswordService _master;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _showPassword;

    public LoginViewModel(IMasterPasswordService master)
    {
        _master = master;
    }

    public bool IsSetupMode => !_master.HasMasterPassword;

    [RelayCommand]
    private void ToggleShowPassword() => ShowPassword = !ShowPassword;

    [RelayCommand]
    private void Confirm(Window? window)
    {
        if (window is null)
        {
            return;
        }

        ErrorMessage = null;
        if (IsSetupMode)
        {
            if (!_master.TryCreateMasterPassword(Password, ConfirmPassword, out var err))
            {
                ErrorMessage = err;
                return;
            }
        }
        else
        {
            if (!_master.TryUnlock(Password, out var err))
            {
                ErrorMessage = err;
                return;
            }
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
