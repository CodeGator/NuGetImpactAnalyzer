using Microsoft.Extensions.DependencyInjection;
using NuGetImpactAnalyzer.Infrastructure;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;
using NuGetImpactAnalyzer.ViewModels;
using NuGetImpactAnalyzer.Views;

namespace NuGetImpactAnalyzer.Tests.Infrastructure;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddNuGetImpactAnalyzerServices_ResolvesCoreServicesAndMainViewModel()
    {
        var services = new ServiceCollection();
        services.AddNuGetImpactAnalyzerServices();
        using var provider = services.BuildServiceProvider();

        var crypto = provider.GetRequiredService<IMasterPasswordCrypto>();
        var masterFileStore = provider.GetRequiredService<IMasterPasswordFileStore>();
        Assert.IsType<MasterPasswordCrypto>(crypto);
        Assert.IsType<MasterPasswordFileStore>(masterFileStore);

        var config = provider.GetRequiredService<IAppConfigurationService>();
        var git = provider.GetRequiredService<IGitService>();
        var catalog = provider.GetRequiredService<IRepositoryCatalogService>();
        var sync = provider.GetRequiredService<IRepositorySyncCoordinator>();
        var parsing = provider.GetRequiredService<IParsingService>();
        var graph = provider.GetRequiredService<IGraphService>();
        var graphBuild = provider.GetRequiredService<IGraphBuildCoordinator>();
        var clock = provider.GetRequiredService<IClock>();
        var parseCache = provider.GetRequiredService<IParseResultCache>();
        var impact = provider.GetRequiredService<IImpactAnalysisService>();
        var buildOrder = provider.GetRequiredService<IBuildOrderService>();
        var analysisReset = provider.GetRequiredService<IAnalysisResetService>();
        var log = provider.GetRequiredService<IApplicationLog>();
        var logVm = provider.GetRequiredService<ApplicationLogViewModel>();
        var dialogs = provider.GetRequiredService<IDialogService>();
        var main = provider.GetRequiredService<MainViewModel>();
        var userPrefs = provider.GetRequiredService<IUserPreferencesService>();
        var impactTargetPrefs = provider.GetRequiredService<IImpactTargetPreferencesService>();
        var appStatus = provider.GetRequiredService<IApplicationStatus>();
        var statusBar = provider.GetRequiredService<StatusBarViewModel>();

        Assert.IsType<JsonFileAppConfigurationService>(config);
        Assert.IsType<JsonUserPreferencesService>(userPrefs);
        Assert.IsType<ImpactTargetPreferencesService>(impactTargetPrefs);
        Assert.Same(statusBar, appStatus);
        Assert.Same(statusBar, main.StatusBar);
        Assert.NotNull(main.Sync);
        Assert.IsType<RepositorySyncViewModel>(main.Sync);
        var gitHubMeta = provider.GetRequiredService<IGitHubRepositoryMetadataService>();
        Assert.IsType<GitHubRepositoryMetadataService>(gitHubMeta);
        Assert.IsType<GitService>(git);
        Assert.IsType<RepositoryCatalogService>(catalog);
        Assert.IsType<RepositorySyncCoordinator>(sync);
        Assert.IsType<ParsingService>(parsing);
        Assert.IsType<GraphService>(graph);
        Assert.IsType<GraphBuildCoordinator>(graphBuild);
        Assert.IsType<SystemClock>(clock);
        Assert.IsType<JsonParseResultCache>(parseCache);
        Assert.IsType<ImpactAnalysisService>(impact);
        Assert.IsType<BuildOrderService>(buildOrder);
        Assert.IsType<AnalysisResetService>(analysisReset);
        var interaction = provider.GetRequiredService<IImpactAnalysisInteractionService>();
        Assert.IsType<ImpactAnalysisInteractionService>(interaction);
        Assert.Same(logVm, log);
        Assert.IsType<DialogService>(dialogs);
        Assert.NotNull(main.Workspace);
        Assert.NotNull(main.Impact.BuildOrder);
        Assert.NotNull(main.Graph);
        Assert.NotNull(main.Impact);
    }

    [Fact]
    public void AddNuGetImpactAnalyzerServices_MainViewModelSyncUsesSameWorkspaceAsShell()
    {
        var services = new ServiceCollection();
        services.AddNuGetImpactAnalyzerServices();
        using var provider = services.BuildServiceProvider();

        var main = provider.GetRequiredService<MainViewModel>();
        Assert.Same(main.Workspace, main.Sync.Workspace);
    }

    [Fact]
    public void AddNuGetImpactAnalyzerServices_IRepositoryCredentialsDialogLauncher_IsDialogServiceInstance()
    {
        var services = new ServiceCollection();
        services.AddNuGetImpactAnalyzerServices();
        using var provider = services.BuildServiceProvider();

        var concrete = provider.GetRequiredService<DialogService>();
        var launcher = provider.GetRequiredService<IRepositoryCredentialsDialogLauncher>();

        Assert.Same(concrete, launcher);
    }

    [Fact]
    public void AddNuGetImpactAnalyzerServices_RepositoryListConfigurationSynchronizer_IsTransient()
    {
        var services = new ServiceCollection();
        services.AddNuGetImpactAnalyzerServices();
        using var provider = services.BuildServiceProvider();

        var a = provider.GetRequiredService<IRepositoryListConfigurationSynchronizer>();
        var b = provider.GetRequiredService<IRepositoryListConfigurationSynchronizer>();

        Assert.NotSame(a, b);
    }

    [Fact]
    public void AddNuGetImpactAnalyzerServices_RegistersMainWindowAsTransient()
    {
        var services = new ServiceCollection();
        services.AddNuGetImpactAnalyzerServices();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(MainWindow));
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void AddNuGetImpactAnalyzerServices_MainViewModelGetsFreshInstancePerResolution()
    {
        var services = new ServiceCollection();
        services.AddNuGetImpactAnalyzerServices();
        using var provider = services.BuildServiceProvider();

        var a = provider.GetRequiredService<MainViewModel>();
        var b = provider.GetRequiredService<MainViewModel>();

        Assert.NotSame(a, b);
    }

    [Fact]
    public void AddNuGetImpactAnalyzerServices_ApplicationLogViewModelIsSingletonSharedByMainViewModels()
    {
        var services = new ServiceCollection();
        services.AddNuGetImpactAnalyzerServices();
        using var provider = services.BuildServiceProvider();

        var log1 = provider.GetRequiredService<MainViewModel>().Log;
        var log2 = provider.GetRequiredService<MainViewModel>().Log;

        Assert.Same(log1, log2);
    }
}
