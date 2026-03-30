namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// Carries the outcome of a view-model–driven dialog close request (view sets <see cref="System.Windows.Window.DialogResult"/>).
/// </summary>
public sealed class DialogResultRequestedEventArgs : EventArgs
{
    public DialogResultRequestedEventArgs(bool? dialogResult) => DialogResult = dialogResult;

    public bool? DialogResult { get; }
}
