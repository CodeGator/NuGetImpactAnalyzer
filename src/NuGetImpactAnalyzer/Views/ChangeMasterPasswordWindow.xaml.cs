using System.Windows;
using System.Windows.Controls;
using NuGetImpactAnalyzer.ViewModels;

namespace NuGetImpactAnalyzer.Views;

public partial class ChangeMasterPasswordWindow : Window
{
    public ChangeMasterPasswordWindow(ChangeMasterPasswordViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void PwCurrent_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChangeMasterPasswordViewModel vm && sender is PasswordBox pb)
        {
            vm.CurrentPassword = pb.Password;
        }
    }

    private void PwNew_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChangeMasterPasswordViewModel vm && sender is PasswordBox pb)
        {
            vm.NewPassword = pb.Password;
        }
    }

    private void PwConfirm_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChangeMasterPasswordViewModel vm && sender is PasswordBox pb)
        {
            vm.ConfirmPassword = pb.Password;
        }
    }
}
