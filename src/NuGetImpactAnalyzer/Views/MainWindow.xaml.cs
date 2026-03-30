using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NuGetImpactAnalyzer.Infrastructure;
using NuGetImpactAnalyzer.Services.Abstractions;
using NuGetImpactAnalyzer.ViewModels;

namespace NuGetImpactAnalyzer.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel, IApplicationSessionController session)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) =>
        {
            WindowPlacement.CenterOnMonitorContainingCursor(this);
            session.AttachMainWindow(this);
        };
        Closed += (_, _) => viewModel.Impact.Dispose();
    }

    /// <summary>
    /// Settings repo grid is display/edit only.
    /// Clear selection after the grid applies it (sync UnselectAll often loses to layout) and drop current cell.
    /// </summary>
    private void SettingsRepositoriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid dg || dg.SelectedItems.Count == 0)
        {
            return;
        }

        dg.Dispatcher.BeginInvoke(
            () =>
            {
                dg.UnselectAll();
                dg.CurrentCell = default;
            },
            DispatcherPriority.Background);
    }
}
