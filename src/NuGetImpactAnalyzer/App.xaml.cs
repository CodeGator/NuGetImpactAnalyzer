using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NuGetImpactAnalyzer.Infrastructure;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;
using NuGetImpactAnalyzer.Views;

namespace NuGetImpactAnalyzer;

/// <summary>
/// WPF application entry point; wires DI and controls the login/logout loop.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Initializes the application with an explicit shutdown mode so the login loop can reopen the main window.
    /// </summary>
    public App()
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    /// <summary>
    /// Starts the app session: shows login, then main window, repeating on logout.
    /// </summary>
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        AppDataLocations.TryMigrateLegacyLayoutOnce();

        var services = new ServiceCollection();
        services.AddNuGetImpactAnalyzerServices();
        _serviceProvider = services.BuildServiceProvider();

        var session = _serviceProvider.GetRequiredService<IApplicationSessionController>();
        var master = _serviceProvider.GetRequiredService<IMasterPasswordService>();

        while (true)
        {
            session.ClearLogoutRequest();

            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            if (loginWindow.ShowDialog() != true)
            {
                break;
            }

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            Application.Current.MainWindow = mainWindow;
            mainWindow.ShowDialog();
            master.Lock();

            if (!session.LogoutRequested)
            {
                break;
            }
        }

        Shutdown();
    }

    /// <summary>
    /// Disposes the application service provider.
    /// </summary>
    /// <param name="e">Shutdown event arguments.</param>
    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
