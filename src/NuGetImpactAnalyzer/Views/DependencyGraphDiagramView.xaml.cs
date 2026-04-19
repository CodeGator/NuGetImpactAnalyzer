using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CodeGator.Wpf;
using NuGetImpactAnalyzer.ViewModels;

namespace NuGetImpactAnalyzer.Views;

/// <summary>
/// Hosts a <see cref="CgDiagram"/> bound to <see cref="DependencyGraphViewModel"/> diagram collections and
/// restarts the force-directed simulation when those collections change.
/// </summary>
public partial class DependencyGraphDiagramView : UserControl
{
    private DependencyGraphViewModel? _hookedVm;

    public DependencyGraphDiagramView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    void OnPrintDiagramClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DependencyGraphViewModel vm || vm.DiagramNodes.Count == 0)
        {
            return;
        }

        Diagram.Print("NuGet Impact Analyzer - dependency graph");
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TryHook(DataContext as DependencyGraphViewModel);
        ScheduleLayout();
        RefreshPrintButtonState();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Unhook();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Unhook();
        TryHook(e.NewValue as DependencyGraphViewModel);
        ScheduleLayout();
        RefreshPrintButtonState();
    }

    private void TryHook(DependencyGraphViewModel? vm)
    {
        if (vm is null || ReferenceEquals(_hookedVm, vm))
        {
            return;
        }

        _hookedVm = vm;
        vm.DiagramNodes.CollectionChanged += OnDiagramCollectionChanged;
        vm.DiagramEdges.CollectionChanged += OnDiagramCollectionChanged;
    }

    private void Unhook()
    {
        if (_hookedVm is null)
        {
            return;
        }

        _hookedVm.DiagramNodes.CollectionChanged -= OnDiagramCollectionChanged;
        _hookedVm.DiagramEdges.CollectionChanged -= OnDiagramCollectionChanged;
        _hookedVm = null;
    }

    private void OnDiagramCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        ScheduleLayout();
        RefreshPrintButtonState();
    }

    void RefreshPrintButtonState()
    {
        PrintDiagramButton.IsEnabled = _hookedVm is not null && _hookedVm.DiagramNodes.Count > 0;
    }

    private void ScheduleLayout()
    {
        Dispatcher.BeginInvoke(new Action(ApplyDiagramLayout), DispatcherPriority.Loaded);
    }

    private void ApplyDiagramLayout()
    {
        if (!IsLoaded || DataContext is not DependencyGraphViewModel vm)
        {
            return;
        }

        if (vm.DiagramNodes.Count == 0)
        {
            return;
        }

        Diagram.ResetSimulation();
    }
}
