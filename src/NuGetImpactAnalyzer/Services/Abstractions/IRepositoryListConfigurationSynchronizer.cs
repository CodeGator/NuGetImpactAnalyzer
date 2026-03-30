using System.Collections.ObjectModel;
using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Keeps <see cref="config.json"/> in sync with an in-memory repository list by reacting to collection and property changes.
/// </summary>
public interface IRepositoryListConfigurationSynchronizer
{
    /// <summary>
    /// Subscribes to <paramref name="repositories"/> for automatic persistence. Call once per instance.
    /// </summary>
    void Bind(ObservableCollection<Repo> repositories);

    /// <summary>
    /// Runs <paramref name="action"/> while suppressing persistence (e.g. reload from disk).
    /// </summary>
    void ExecuteWhileSilent(Action action);
}
