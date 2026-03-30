using System.Windows;

namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Coordinates main-window lifetime so the shell can log out and return to the login prompt.
/// </summary>
public interface IApplicationSessionController
{
    /// <summary>Register the app main window (typically on Loaded).</summary>
    void AttachMainWindow(Window window);

    /// <summary>Set when the user chooses Log out; cleared when starting a new login cycle.</summary>
    bool LogoutRequested { get; }

    void RequestLogout();

    void ClearLogoutRequest();
}
