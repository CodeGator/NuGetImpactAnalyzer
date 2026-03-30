using System.Windows;
using NuGetImpactAnalyzer.ViewModels;

namespace NuGetImpactAnalyzer.Views;

public partial class EditRepositoryWindow : Window
{
    public EditRepositoryWindow(EditRepositoryViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, DialogResultRequestedEventArgs e)
    {
        DialogResult = e.DialogResult;
    }
}
