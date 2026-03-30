using System.Windows;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Infrastructure;

/// <inheritdoc />
public sealed class ApplicationSessionController : IApplicationSessionController
{
    private WeakReference<Window>? _mainWindow;

    /// <inheritdoc />
    public bool LogoutRequested { get; private set; }

    /// <inheritdoc />
    public void AttachMainWindow(Window window) => _mainWindow = new WeakReference<Window>(window);

    /// <inheritdoc />
    public void RequestLogout()
    {
        LogoutRequested = true;
        if (_mainWindow?.TryGetTarget(out var w) == true)
        {
            w.Close();
        }
    }

    /// <inheritdoc />
    public void ClearLogoutRequest()
    {
        LogoutRequested = false;
        _mainWindow = null;
    }
}
