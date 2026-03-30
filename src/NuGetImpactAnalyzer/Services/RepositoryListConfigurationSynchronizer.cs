using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

/// <summary>
/// Watches <see cref="Repo"/> property changes and collection mutations to persist via <see cref="IRepositoryCatalogService.Persist"/>.
/// </summary>
public sealed class RepositoryListConfigurationSynchronizer : IRepositoryListConfigurationSynchronizer
{
    private readonly IRepositoryCatalogService _catalog;
    private readonly IApplicationLog _log;
    private readonly HashSet<Repo> _attachedRepos = [];

    private ObservableCollection<Repo>? _bound;
    private bool _suppress;

    public RepositoryListConfigurationSynchronizer(IRepositoryCatalogService catalog, IApplicationLog log)
    {
        _catalog = catalog;
        _log = log;
    }

    /// <inheritdoc />
    public void Bind(ObservableCollection<Repo> repositories)
    {
        if (_bound is not null)
        {
            throw new InvalidOperationException($"{nameof(Bind)} may only be called once per {nameof(RepositoryListConfigurationSynchronizer)} instance.");
        }

        _bound = repositories;
        _bound.CollectionChanged += OnCollectionChanged;
        foreach (var r in _bound)
        {
            AttachPropertyChanged(r);
        }
    }

    /// <inheritdoc />
    public void ExecuteWhileSilent(Action action)
    {
        _suppress = true;
        try
        {
            action();
        }
        finally
        {
            _suppress = false;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var r in _attachedRepos.ToArray())
            {
                DetachPropertyChanged(r);
            }

            _attachedRepos.Clear();
            if (_bound is not null)
            {
                foreach (var r in _bound)
                {
                    AttachPropertyChanged(r);
                }
            }
        }
        else
        {
            if (e.NewItems is not null)
            {
                foreach (Repo r in e.NewItems)
                {
                    AttachPropertyChanged(r);
                }
            }

            if (e.OldItems is not null)
            {
                foreach (Repo r in e.OldItems)
                {
                    DetachPropertyChanged(r);
                }
            }
        }

        PersistIfAllowed();
    }

    private void AttachPropertyChanged(Repo repo)
    {
        if (!_attachedRepos.Add(repo))
        {
            return;
        }

        repo.PropertyChanged += OnRepoPropertyChanged;
    }

    private void DetachPropertyChanged(Repo repo)
    {
        if (!_attachedRepos.Remove(repo))
        {
            return;
        }

        repo.PropertyChanged -= OnRepoPropertyChanged;
    }

    private void OnRepoPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null ||
            e.PropertyName is nameof(Repo.Name) or nameof(Repo.Url) or nameof(Repo.Branch) or
            nameof(Repo.AnalysisProjectRelativePath))
        {
            PersistIfAllowed();
        }
    }

    private void PersistIfAllowed()
    {
        if (_suppress || _bound is null)
        {
            return;
        }

        try
        {
            _catalog.Persist(_bound);
        }
        catch (Exception ex)
        {
            _log.AppendLine($"Could not save config.json: {ex.Message}");
        }
    }
}
