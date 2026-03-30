using System.Collections.ObjectModel;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class RepositoryCatalogServiceTests
{
    private sealed class FakeAppConfiguration : IAppConfigurationService
    {
        public required ConfigurationLoadResult NextResult { get; init; }

        public List<AppConfig> Saved { get; } = [];

        public ConfigurationLoadResult Load() => NextResult;

        public void Save(AppConfig config) => Saved.Add(new AppConfig { Repos = [.. config.Repos] });
    }

    [Fact]
    public void Refresh_ReplacesCollectionWithReposFromConfiguration()
    {
        var repo = new Repo { Name = "Alpha", Url = "https://example.com/a.git", Branch = "develop" };
        var fake = new FakeAppConfiguration
        {
            NextResult = new ConfigurationLoadResult(
                new AppConfig { Repos = [repo] },
                Warning: null),
        };
        var sut = new RepositoryCatalogService(fake);
        var collection = new ObservableCollection<Repo>
        {
            new() { Name = "Stale", Url = "https://old", Branch = "main" },
        };

        var returned = sut.Refresh(collection);

        Assert.Single(collection);
        Assert.Same(repo, collection[0]);
        Assert.Same(repo, returned.Config.Repos[0]);
        Assert.Null(returned.Warning);
    }

    [Fact]
    public void Refresh_WhenConfigEmpty_ClearsCollection()
    {
        var fake = new FakeAppConfiguration
        {
            NextResult = new ConfigurationLoadResult(new AppConfig(), Warning: null),
        };
        var sut = new RepositoryCatalogService(fake);
        var collection = new ObservableCollection<Repo>
        {
            new() { Name = "X", Url = "u", Branch = "main" },
        };

        var returned = sut.Refresh(collection);

        Assert.Empty(collection);
        Assert.Empty(returned.Config.Repos);
    }

    [Fact]
    public void Refresh_ReturnsWarningFromConfiguration()
    {
        var fake = new FakeAppConfiguration
        {
            NextResult = new ConfigurationLoadResult(new AppConfig(), Warning: "missing file"),
        };
        var sut = new RepositoryCatalogService(fake);
        var collection = new ObservableCollection<Repo>();

        var returned = sut.Refresh(collection);

        Assert.Equal("missing file", returned.Warning);
    }

    [Fact]
    public void Refresh_WhenMultipleRepos_PreservesConfigurationOrder()
    {
        var repos = new[]
        {
            new Repo { Name = "First", Url = "https://a.git", Branch = "main" },
            new Repo { Name = "Second", Url = "https://b.git", Branch = "develop" },
            new Repo { Name = "Third", Url = "https://c.git", Branch = "main" },
        };
        var fake = new FakeAppConfiguration
        {
            NextResult = new ConfigurationLoadResult(new AppConfig { Repos = [.. repos] }, Warning: null),
        };
        var sut = new RepositoryCatalogService(fake);
        var collection = new ObservableCollection<Repo>();

        sut.Refresh(collection);

        Assert.Equal(3, collection.Count);
        Assert.Same(repos[0], collection[0]);
        Assert.Same(repos[1], collection[1]);
        Assert.Same(repos[2], collection[2]);
    }

    [Fact]
    public void Persist_WritesCurrentCollectionThroughConfiguration()
    {
        var fake = new FakeAppConfiguration
        {
            NextResult = new ConfigurationLoadResult(new AppConfig(), Warning: null),
        };
        var sut = new RepositoryCatalogService(fake);
        var collection = new ObservableCollection<Repo>
        {
            new() { Name = "A", Url = "https://a.git", Branch = "main" },
            new() { Name = "B", Url = "https://b.git", Branch = "develop" },
        };

        sut.Persist(collection);

        Assert.Single(fake.Saved);
        Assert.Equal(2, fake.Saved[0].Repos.Count);
        Assert.Equal("A", fake.Saved[0].Repos[0].Name);
        Assert.Equal("B", fake.Saved[0].Repos[1].Name);
    }
}
