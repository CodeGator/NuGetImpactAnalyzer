using System.Collections.ObjectModel;
using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Loads configured repositories into a live collection (e.g. from config.json).
/// </summary>
public interface IRepositoryCatalogService
{
    /// <summary>
    /// Replaces <paramref name="repositories"/> with the current configuration and returns the load outcome.
    /// </summary>
    ConfigurationLoadResult Refresh(ObservableCollection<Repo> repositories);

    /// <summary>
    /// Writes <paramref name="repositories"/> to application configuration storage in their current order.
    /// </summary>
    void Persist(ObservableCollection<Repo> repositories);
}
