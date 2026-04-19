using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuGetImpactAnalyzer.Infrastructure;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly IMasterPasswordService _master;
    private readonly IVaultSecretsResetService _vaultReset;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _showPassword;

    public LoginViewModel(IMasterPasswordService master, IVaultSecretsResetService vaultSecretsReset)
    {
        _master = master;
        _vaultReset = vaultSecretsReset;
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

    [RelayCommand]
    private void ForgotMasterPassword(Window? window)
    {
        if (window is null || IsSetupMode)
        {
            return;
        }

        const string message =
            "This removes your master password record and all stored GitHub personal access tokens from this computer. "
            + "You cannot recover those tokens here. Your repository list, preferences, analysis cache, and cloned repos are not deleted.\n\n"
            + "Continue?";

        if (CenteredMessageBox.Show(
                window,
                message,
                "Reset vault",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        if (!_vaultReset.TryResetAfterForgottenMasterPassword(out var err))
        {
            ErrorMessage = err ?? "Vault reset failed.";
            return;
        }

        Password = string.Empty;
        ConfirmPassword = string.Empty;
        ErrorMessage = null;
        OnPropertyChanged(nameof(IsSetupMode));
    }
}
