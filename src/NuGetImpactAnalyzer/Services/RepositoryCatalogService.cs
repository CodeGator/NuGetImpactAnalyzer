using System.Collections.ObjectModel;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

public sealed class RepositoryCatalogService : IRepositoryCatalogService
{
    private readonly IAppConfigurationService _appConfiguration;

    public RepositoryCatalogService(IAppConfigurationService appConfiguration)
    {
        _appConfiguration = appConfiguration;
    }

    /// <inheritdoc />
    public ConfigurationLoadResult Refresh(ObservableCollection<Repo> repositories)
    {
        var result = _appConfiguration.Load();

        repositories.Clear();
        foreach (var repo in result.Config.Repos)
        {
            repositories.Add(repo);
        }

        return result;
    }

    /// <inheritdoc />
    public void Persist(ObservableCollection<Repo> repositories)
    {
        var config = new AppConfig { Repos = [.. repositories] };
        _appConfiguration.Save(config);
    }
}
