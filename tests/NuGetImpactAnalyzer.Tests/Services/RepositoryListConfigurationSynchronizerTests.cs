using System.Collections.ObjectModel;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class RepositoryListConfigurationSynchronizerTests
{
    private sealed class CountingCatalog : IRepositoryCatalogService
    {
        public int PersistCallCount { get; private set; }

        public ConfigurationLoadResult Refresh(ObservableCollection<Repo> repositories)
        {
            repositories.Clear();
            return new ConfigurationLoadResult(new AppConfig(), Warning: null);
        }

        public void Persist(ObservableCollection<Repo> repositories) => PersistCallCount++;
    }

    private sealed class ThrowingCatalog : IRepositoryCatalogService
    {
        public ConfigurationLoadResult Refresh(ObservableCollection<Repo> repositories) =>
            new(new AppConfig(), Warning: null);

        public void Persist(ObservableCollection<Repo> repositories) =>
            throw new IOException("simulated failure");
    }

    private sealed class CaptureLog : IApplicationLog
    {
        public List<string> Lines { get; } = [];

        public void AppendLine(string line) => Lines.Add(line);
    }

    [Fact]
    public void Bind_CalledTwice_ThrowsInvalidOperationException()
    {
        var sut = new RepositoryListConfigurationSynchronizer(new CountingCatalog(), new CaptureLog());
        var coll = new ObservableCollection<Repo>();

        sut.Bind(coll);

        Assert.Throws<InvalidOperationException>(() => sut.Bind(coll));
    }

    [Fact]
    public void AfterBind_RepoPropertyChange_CallsPersist()
    {
        var catalog = new CountingCatalog();
        var sut = new RepositoryListConfigurationSynchronizer(catalog, new CaptureLog());
        var repo = new Repo { Name = "R1", Url = "https://example.com/r.git", Branch = "main" };
        var coll = new ObservableCollection<Repo> { repo };

        sut.Bind(coll);
        Assert.Equal(0, catalog.PersistCallCount);

        repo.Name = "Renamed";

        Assert.Equal(1, catalog.PersistCallCount);
    }

    [Fact]
    public void AfterBind_CollectionAdd_CallsPersist()
    {
        var catalog = new CountingCatalog();
        var sut = new RepositoryListConfigurationSynchronizer(catalog, new CaptureLog());
        var coll = new ObservableCollection<Repo>();

        sut.Bind(coll);
        coll.Add(new Repo { Name = "New", Url = "https://n.git", Branch = "main" });

        Assert.Equal(1, catalog.PersistCallCount);
    }

    [Fact]
    public void AfterBind_CollectionRemove_CallsPersist()
    {
        var catalog = new CountingCatalog();
        var sut = new RepositoryListConfigurationSynchronizer(catalog, new CaptureLog());
        var r = new Repo { Name = "X", Url = "u", Branch = "main" };
        var coll = new ObservableCollection<Repo> { r };

        sut.Bind(coll);
        Assert.Equal(0, catalog.PersistCallCount);

        coll.Remove(r);

        Assert.Equal(1, catalog.PersistCallCount);
    }

    [Fact]
    public void ExecuteWhileSilent_SuppressesPersist_ForCollectionAndPropertyChanges()
    {
        var catalog = new CountingCatalog();
        var sut = new RepositoryListConfigurationSynchronizer(catalog, new CaptureLog());
        var coll = new ObservableCollection<Repo>();

        sut.Bind(coll);
        var added = new Repo { Name = "A", Url = "u", Branch = "b" };

        sut.ExecuteWhileSilent(() =>
        {
            coll.Add(added);
            added.Name = "EditedUnderSilent";
        });

        Assert.Equal(0, catalog.PersistCallCount);

        added.Branch = "topic";

        Assert.Equal(1, catalog.PersistCallCount);
    }

    [Fact]
    public void Persist_WhenCatalogThrows_AppendsToLog()
    {
        var catalog = new ThrowingCatalog();
        var log = new CaptureLog();
        var sut = new RepositoryListConfigurationSynchronizer(catalog, log);
        var repo = new Repo { Name = "R", Url = "u", Branch = "main" };
        var coll = new ObservableCollection<Repo> { repo };

        sut.Bind(coll);
        repo.Url = "https://other.git";

        Assert.Contains(log.Lines, l => l.Contains("Could not save config.json", StringComparison.Ordinal));
        Assert.Contains(log.Lines, l => l.Contains("simulated failure", StringComparison.Ordinal));
    }

    [Fact]
    public void CollectionReset_WhenNotSilent_CallsPersist()
    {
        var catalog = new CountingCatalog();
        var sut = new RepositoryListConfigurationSynchronizer(catalog, new CaptureLog());
        var coll = new ObservableCollection<Repo>
        {
            new() { Name = "A", Url = "u", Branch = "main" },
        };

        sut.Bind(coll);
        coll.Clear();

        Assert.Equal(1, catalog.PersistCallCount);
    }
}
