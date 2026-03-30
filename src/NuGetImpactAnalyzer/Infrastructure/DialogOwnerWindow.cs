using System.Windows;

namespace NuGetImpactAnalyzer.Infrastructure;

/// <summary>
/// Resolves a <see cref="Window"/> suitable as <c>Owner</c> for modal dialogs and
/// WPF <see cref="MessageBox"/> so they center over the application instead of the screen.
/// </summary>
public static class DialogOwnerWindow
{
    /// <summary>
    /// Prefers <see cref="Application.MainWindow"/> when loaded and visible; otherwise the active visible window.
    /// </summary>
    public static Window? Resolve()
    {
        var app = Application.Current;
        if (app is null)
        {
            return null;
        }

        if (app.MainWindow is { IsLoaded: true } main && main.IsVisible)
        {
            return main;
        }

        foreach (Window w in app.Windows)
        {
            if (w is { IsLoaded: true, IsVisible: true } && w.IsActive)
            {
                return w;
            }
        }

        foreach (Window w in app.Windows)
        {
            if (w is { IsLoaded: true, IsVisible: true })
            {
                return w;
            }
        }

        return null;
    }
}
