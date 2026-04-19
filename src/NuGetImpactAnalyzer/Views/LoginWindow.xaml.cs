using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NuGetImpactAnalyzer.Infrastructure;
using NuGetImpactAnalyzer.ViewModels;

namespace NuGetImpactAnalyzer.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        Loaded += (_, _) =>
        {
            WindowPlacement.CenterOnMonitorContainingCursor(this);
            FocusInitialPasswordField();
        };
        viewModel.PropertyChanged += LoginViewModel_OnPropertyChanged;
        Closed += (_, _) => viewModel.PropertyChanged -= LoginViewModel_OnPropertyChanged;
    }

    private void FocusInitialPasswordField()
    {
        // Focus the currently-visible password input.
        if (DataContext is LoginViewModel vm && vm.ShowPassword)
        {
            PasswordTextMain.Focus();
        }
        else
        {
            PasswordBoxMain.Focus();
        }
    }

    private void LoginViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not LoginViewModel vm)
        {
            return;
        }

        if (e.PropertyName == nameof(LoginViewModel.ShowPassword))
        {
            if (!vm.ShowPassword)
            {
                PasswordBoxMain.Password = vm.Password;
                PasswordBoxConfirm.Password = vm.ConfirmPassword;
            }
        }
        else if (e.PropertyName == nameof(LoginViewModel.Password) && string.IsNullOrEmpty(vm.Password))
        {
            if (PasswordBoxMain.Password.Length > 0)
            {
                PasswordBoxMain.Password = string.Empty;
            }
        }
        else if (e.PropertyName == nameof(LoginViewModel.ConfirmPassword) && string.IsNullOrEmpty(vm.ConfirmPassword))
        {
            if (PasswordBoxConfirm.Password.Length > 0)
            {
                PasswordBoxConfirm.Password = string.Empty;
            }
        }
        else if (e.PropertyName == nameof(LoginViewModel.IsSetupMode) && vm.IsSetupMode && IsLoaded)
        {
            FocusInitialPasswordField();
        }
    }

    private void PasswordBoxMain_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
        {
            vm.Password = pb.Password;
        }
    }

    private void PasswordBoxConfirm_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox pb)
        {
            vm.ConfirmPassword = pb.Password;
        }
    }
}
