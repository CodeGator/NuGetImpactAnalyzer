using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Net.Http;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;
using NuGetImpactAnalyzer.ViewModels;
using NuGetImpactAnalyzer.Views;

namespace NuGetImpactAnalyzer.Infrastructure;

/// <summary>
/// Composition root for application services and view models.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <param name="appStorageDirectory">Optional root for master password file, token index, config, preferences, and cache. Defaults to per-user local app data (not the install folder).</param>
    public static IServiceCollection AddNuGetImpactAnalyzerServices(
        this IServiceCollection services,
        string? appStorageDirectory = null)
    {
        var storage = appStorageDirectory ?? MasterPasswordFileStore.DefaultStorageDirectory();
        var reposRoot = Path.Combine(storage, "repos");
        services.AddSingleton(_ => new TokenStorageIndex(storage));
        services.AddSingleton<WindowsCredentialStore>();
        services.AddSingleton<IMasterPasswordPolicy, MasterPasswordPolicy>();
        services.AddSingleton<IMasterPasswordCrypto, MasterPasswordCrypto>();
        services.AddSingleton<IMasterPasswordFileStore>(_ => new MasterPasswordFileStore(storage));
        services.AddSingleton<IStoredTokenRewrapper, WindowsCredentialTokenRewrapper>();
        services.AddSingleton<IMasterPasswordService, MasterPasswordService>();
        services.AddSingleton<ICredentialService, ProtectedCredentialService>();
        services.AddSingleton<IApplicationSessionController, ApplicationSessionController>();

        services.AddSingleton<IAppConfigurationService>(_ => new JsonFileAppConfigurationService(Path.Combine(storage, "config.json")));
        services.AddSingleton<IUserPreferencesService>(_ => new JsonUserPreferencesService(Path.Combine(storage, "userpreferences.json")));
        services.AddSingleton<IImpactTargetPreferencesService, ImpactTargetPreferencesService>();
        services.AddSingleton<StatusBarViewModel>();
        services.AddSingleton<IApplicationStatus>(sp => sp.GetRequiredService<StatusBarViewModel>());
        services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(25) });
        services.AddSingleton<IGitHubRepositoryMetadataService, GitHubRepositoryMetadataService>();
        services.AddSingleton<IGitService>(sp => new GitService(reposRoot, sp.GetRequiredService<ICredentialService>()));
        services.AddSingleton<IRepositoryCatalogService, RepositoryCatalogService>();
        services.AddSingleton<IRepositorySyncCoordinator, RepositorySyncCoordinator>();
        services.AddSingleton<IParsingService, ParsingService>();
        services.AddSingleton<IParseResultCache>(_ => new JsonParseResultCache(Path.Combine(storage, "cache.json")));
        services.AddSingleton<IGraphService, GraphService>();
        services.AddSingleton<IGraphBuildCoordinator, GraphBuildCoordinator>();
        services.AddSingleton<IImpactAnalysisService, ImpactAnalysisService>();
        services.AddSingleton<IBuildOrderService, BuildOrderService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IAnalysisResetService, AnalysisResetService>();
        services.AddSingleton<IImpactAnalysisInteractionService, ImpactAnalysisInteractionService>();

        services.AddSingleton<DialogService>();
        services.AddSingleton<IDialogService>(sp => sp.GetRequiredService<DialogService>());
        services.AddSingleton<IRepositoryCredentialsDialogLauncher>(sp => sp.GetRequiredService<DialogService>());
        services.AddSingleton<IDialogViewModelFactory, DialogViewModelFactory>();

        services.AddSingleton<ApplicationLogViewModel>();
        services.AddSingleton<IApplicationLog>(sp => sp.GetRequiredService<ApplicationLogViewModel>());

        services.AddTransient<IRepositoryListConfigurationSynchronizer, RepositoryListConfigurationSynchronizer>();
        services.AddTransient<RepositoryWorkspaceViewModel>();
        services.AddTransient<DependencyGraphViewModel>();
        services.AddTransient<ImpactBuildOrderViewModel>();
        services.AddTransient<ImpactAnalysisViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginWindow>();

        // One workspace instance must back both the bound grid (MainViewModel.Workspace) and sync commands
        // (RepositorySyncViewModel); transient per-parameter resolution would create two unrelated workspaces.
        services.AddTransient<MainViewModel>(sp =>
        {
            var workspace = ActivatorUtilities.CreateInstance<RepositoryWorkspaceViewModel>(sp);
            var sync = ActivatorUtilities.CreateInstance<RepositorySyncViewModel>(sp, workspace);
            var graph = sp.GetRequiredService<DependencyGraphViewModel>();
            var impact = sp.GetRequiredService<ImpactAnalysisViewModel>();
            var log = sp.GetRequiredService<ApplicationLogViewModel>();
            var statusBar = sp.GetRequiredService<StatusBarViewModel>();
            var master = sp.GetRequiredService<IMasterPasswordService>();
            var session = sp.GetRequiredService<IApplicationSessionController>();
            return new MainViewModel(workspace, sync, graph, impact, log, statusBar, master, session);
        });
        services.AddTransient<MainWindow>();

        return services;
    }
}
