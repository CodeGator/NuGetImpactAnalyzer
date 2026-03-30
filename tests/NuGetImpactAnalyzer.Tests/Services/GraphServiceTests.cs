using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class GraphServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public GraphServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "nuget-graph-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // best-effort
        }
    }

    private sealed class FakeAppConfiguration : IAppConfigurationService
    {
        public required ConfigurationLoadResult Next { get; init; }

        public ConfigurationLoadResult Load() => Next;

        public void Save(AppConfig config) { }
    }

    private sealed class FakeParsingService : IParsingService
    {
        public required IReadOnlyDictionary<string, IReadOnlyList<ProjectInfo>> ProjectsByRepoName { get; init; }

        public Task<IReadOnlyList<ProjectInfo>> AnalyzeRepositoryAsync(Repo repo, CancellationToken cancellationToken = default)
        {
            if (ProjectsByRepoName.TryGetValue(repo.Name, out var list))
            {
                return Task.FromResult(list);
            }

            return Task.FromResult<IReadOnlyList<ProjectInfo>>([]);
        }
    }

    private sealed class FakeGitService : IGitService
    {
        /// <summary>When set, <see cref="GetLocalRepositoryPath"/> returns this for every repo (for analysis-scope tests).</summary>
        public string? LocalRepositoryRoot { get; init; }

        public string GetLocalRepositoryPath(Repo repo) =>
            LocalRepositoryRoot ?? Path.Combine(Path.GetTempPath(), "nuget-impact-graph-fake-" + repo.Name);

        public string? TryGetHeadCommitSha(string localRepositoryPath) => null;

        public Task CloneOrUpdateAsync(Repo repo, IProgress<string>? progress = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SyncAllAsync(IEnumerable<Repo> repos, IProgress<string>? progress = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public IReadOnlyList<string> ListBranches(Repo repo) => [];

        public IReadOnlyList<string> ListProjectRelativePaths(Repo repo) => [];

        public bool TryProbeRemoteRepository(Repo repo) => false;

        public bool TryDeleteLocalClone(Repo repo) => true;
    }

    private static GraphService CreateGraphService(
        FakeAppConfiguration fakeConfig,
        FakeParsingService fakeParse,
        FakeGitService? git = null) =>
        new(fakeConfig, fakeParse, git ?? new FakeGitService());

    [Fact]
    public async Task BuildGraphAsync_LinksProjectReferencesByResolvedPath()
    {
        var libDir = Path.Combine(_tempRoot, "Lib");
        var appDir = Path.Combine(_tempRoot, "App");
        Directory.CreateDirectory(libDir);
        Directory.CreateDirectory(appDir);
        var libCs = Path.Combine(libDir, "Lib.csproj");
        var appCs = Path.Combine(appDir, "App.csproj");
        await File.WriteAllTextAsync(libCs, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        await File.WriteAllTextAsync(appCs, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        var repos = new List<Repo> { new() { Name = "R1", Url = "u", Branch = "main" } };
        var lib = new ProjectInfo
        {
            Name = "Lib",
            FilePath = libCs,
            PackageReferences = [],
            ProjectReferences = [],
        };
        var app = new ProjectInfo
        {
            Name = "App",
            FilePath = appCs,
            PackageReferences = [],
            ProjectReferences = [Path.Combine("..", "Lib", "Lib.csproj")],
        };

        var fakeConfig = new FakeAppConfiguration
        {
            Next = new ConfigurationLoadResult(new AppConfig { Repos = repos }, Warning: null),
        };
        var fakeParse = new FakeParsingService
        {
            ProjectsByRepoName = new Dictionary<string, IReadOnlyList<ProjectInfo>>(StringComparer.OrdinalIgnoreCase)
            {
                ["R1"] = [app, lib],
            },
        };

        var sut = CreateGraphService(fakeConfig, fakeParse);

        await sut.BuildGraphAsync();

        var appId = GraphService.MakeNodeId("R1", "App");
        var libId = GraphService.MakeNodeId("R1", "Lib");
        Assert.True(sut.Nodes.ContainsKey(appId));
        Assert.Contains(libId, sut.Nodes[appId].Dependencies);
    }

    [Fact]
    public async Task BuildGraphAsync_WhenAnalysisScopeSet_IncludesOnlyRootTransitiveClosure()
    {
        var appDir = Path.Combine(_tempRoot, "App");
        var libDir = Path.Combine(_tempRoot, "Lib");
        var utilDir = Path.Combine(_tempRoot, "Util");
        var orphanDir = Path.Combine(_tempRoot, "Orphan");
        Directory.CreateDirectory(appDir);
        Directory.CreateDirectory(libDir);
        Directory.CreateDirectory(utilDir);
        Directory.CreateDirectory(orphanDir);
        var appCs = Path.Combine(appDir, "App.csproj");
        var libCs = Path.Combine(libDir, "Lib.csproj");
        var utilCs = Path.Combine(utilDir, "Util.csproj");
        var orphanCs = Path.Combine(orphanDir, "Orphan.csproj");
        await File.WriteAllTextAsync(utilCs, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        await File.WriteAllTextAsync(libCs, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        await File.WriteAllTextAsync(appCs, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        await File.WriteAllTextAsync(orphanCs, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        var repos = new List<Repo>
        {
            new()
            {
                Name = "R1",
                Url = "u",
                Branch = "main",
                AnalysisProjectRelativePath = "App/App.csproj",
            },
        };
        var orphan = new ProjectInfo
        {
            Name = "Orphan",
            FilePath = orphanCs,
            PackageReferences = [],
            ProjectReferences = [],
        };
        var util = new ProjectInfo
        {
            Name = "Util",
            FilePath = utilCs,
            PackageReferences = [],
            ProjectReferences = [],
        };
        var lib = new ProjectInfo
        {
            Name = "Lib",
            FilePath = libCs,
            PackageReferences = [],
            ProjectReferences = [Path.Combine("..", "Util", "Util.csproj")],
        };
        var app = new ProjectInfo
        {
            Name = "App",
            FilePath = appCs,
            PackageReferences = [],
            ProjectReferences = [Path.Combine("..", "Lib", "Lib.csproj")],
        };

        var fakeConfig = new FakeAppConfiguration
        {
            Next = new ConfigurationLoadResult(new AppConfig { Repos = repos }, Warning: null),
        };
        var fakeParse = new FakeParsingService
        {
            ProjectsByRepoName = new Dictionary<string, IReadOnlyList<ProjectInfo>>(StringComparer.OrdinalIgnoreCase)
            {
                ["R1"] = [app, lib, util, orphan],
            },
        };

        var sut = CreateGraphService(fakeConfig, fakeParse, new FakeGitService { LocalRepositoryRoot = _tempRoot });

        await sut.BuildGraphAsync();

        Assert.True(sut.Nodes.ContainsKey(GraphService.MakeNodeId("R1", "App")));
        Assert.True(sut.Nodes.ContainsKey(GraphService.MakeNodeId("R1", "Lib")));
        Assert.True(sut.Nodes.ContainsKey(GraphService.MakeNodeId("R1", "Util")));
        Assert.False(sut.Nodes.ContainsKey(GraphService.MakeNodeId("R1", "Orphan")));
    }

    [Fact]
    public async Task BuildGraphAsync_ExcludesProjectsWhoseNameContainsTest()
    {
        var libDir = Path.Combine(_tempRoot, "Lib");
        var testDir = Path.Combine(_tempRoot, "UnitTests");
        var appDir = Path.Combine(_tempRoot, "App");
        Directory.CreateDirectory(libDir);
        Directory.CreateDirectory(testDir);
        Directory.CreateDirectory(appDir);
        var libCs = Path.Combine(libDir, "CG.Alerts.csproj");
        var testCs = Path.Combine(testDir, "CG.Alerts.UnitTests.csproj");
        var appCs = Path.Combine(appDir, "App.csproj");
        await File.WriteAllTextAsync(libCs, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        await File.WriteAllTextAsync(testCs, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        await File.WriteAllTextAsync(appCs, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        var repos = new List<Repo> { new() { Name = "CG.Alerts", Url = "u", Branch = "main" } };
        var lib = new ProjectInfo
        {
            Name = "CG.Alerts",
            FilePath = libCs,
            PackageReferences = [],
            ProjectReferences = [],
        };
        var unitTests = new ProjectInfo
        {
            Name = "CG.Alerts.UnitTests",
            FilePath = testCs,
            PackageReferences = [],
            ProjectReferences = [Path.Combine("..", "Lib", "CG.Alerts.csproj")],
        };
        var app = new ProjectInfo
        {
            Name = "App",
            FilePath = appCs,
            PackageReferences = [],
            ProjectReferences =
            [
                Path.Combine("..", "Lib", "CG.Alerts.csproj"),
                Path.Combine("..", "UnitTests", "CG.Alerts.UnitTests.csproj"),
            ],
        };

        var fakeConfig = new FakeAppConfiguration
        {
            Next = new ConfigurationLoadResult(new AppConfig { Repos = repos }, Warning: null),
        };
        var fakeParse = new FakeParsingService
        {
            ProjectsByRepoName = new Dictionary<string, IReadOnlyList<ProjectInfo>>(StringComparer.OrdinalIgnoreCase)
            {
                ["CG.Alerts"] = [app, lib, unitTests],
            },
        };

        var sut = CreateGraphService(fakeConfig, fakeParse);
        await sut.BuildGraphAsync();

        var appId = GraphService.MakeNodeId("CG.Alerts", "App");
        var libId = GraphService.MakeNodeId("CG.Alerts", "CG.Alerts");
        var testId = GraphService.MakeNodeId("CG.Alerts", "CG.Alerts.UnitTests");

        Assert.True(sut.Nodes.ContainsKey(appId));
        Assert.True(sut.Nodes.ContainsKey(libId));
        Assert.False(sut.Nodes.ContainsKey(testId));
        Assert.Contains(libId, sut.Nodes[appId].Dependencies);
        Assert.DoesNotContain(testId, sut.Nodes[appId].Dependencies);
    }

    [Fact]
    public async Task BuildGraphAsync_LinksPackageReferenceWhenProjectNameMatchesInclude()
    {
        var appCs = Path.Combine(_tempRoot, "App.csproj");
        var libCs = Path.Combine(_tempRoot, "Lib.csproj");
        await File.WriteAllTextAsync(libCs, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        await File.WriteAllTextAsync(appCs, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        var repos = new List<Repo> { new() { Name = "R1", Url = "u", Branch = "main" } };
        var lib = new ProjectInfo
        {
            Name = "Company.Core",
            FilePath = libCs,
            PackageReferences = [],
            ProjectReferences = [],
        };
        var app = new ProjectInfo
        {
            Name = "App",
            FilePath = appCs,
            PackageReferences =
            [
                new PackageReferenceInfo { Include = "Company.Core", Version = "1.0.0" },
            ],
            ProjectReferences = [],
        };

        var fakeConfig = new FakeAppConfiguration
        {
            Next = new ConfigurationLoadResult(new AppConfig { Repos = repos }, Warning: null),
        };
        var fakeParse = new FakeParsingService
        {
            ProjectsByRepoName = new Dictionary<string, IReadOnlyList<ProjectInfo>>(StringComparer.OrdinalIgnoreCase)
            {
                ["R1"] = [app, lib],
            },
        };

        var sut = CreateGraphService(fakeConfig, fakeParse);

        await sut.BuildGraphAsync();

        var appId = GraphService.MakeNodeId("R1", "App");
        var libId = GraphService.MakeNodeId("R1", "Company.Core");
        Assert.Contains(libId, sut.Nodes[appId].Dependencies);
    }

    [Fact]
    public async Task GetNode_ReturnsNodeByFullIdAfterBuild()
    {
        var cs = Path.Combine(_tempRoot, "Only.csproj");
        await File.WriteAllTextAsync(cs, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        var repos = new List<Repo> { new() { Name = "RepoX", Url = "u", Branch = "main" } };
        var proj = new ProjectInfo { Name = "Only", FilePath = cs, PackageReferences = [], ProjectReferences = [] };
        var fakeConfig = new FakeAppConfiguration
        {
            Next = new ConfigurationLoadResult(new AppConfig { Repos = repos }, Warning: null),
        };
        var fakeParse = new FakeParsingService
        {
            ProjectsByRepoName = new Dictionary<string, IReadOnlyList<ProjectInfo>>(StringComparer.OrdinalIgnoreCase)
            {
                ["RepoX"] = [proj],
            },
        };
        var sut = CreateGraphService(fakeConfig, fakeParse);
        await sut.BuildGraphAsync();

        var id = GraphService.MakeNodeId("RepoX", "Only");
        var node = sut.GetNode(id);

        Assert.NotNull(node);
        Assert.Equal("Only", node!.Name);
        Assert.Equal("RepoX", node.RepoName);
    }

    [Fact]
    public void FormatGraphText_WhenEmpty_ReturnsGuidanceMessage()
    {
        var fakeConfig = new FakeAppConfiguration
        {
            Next = new ConfigurationLoadResult(new AppConfig { Repos = [] }, Warning: null),
        };
        var fakeParse = new FakeParsingService { ProjectsByRepoName = new Dictionary<string, IReadOnlyList<ProjectInfo>>() };
        var sut = CreateGraphService(fakeConfig, fakeParse);

        var text = sut.FormatGraphText();

        Assert.Contains("No nodes", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FormatGraphText_ShowsArrowLines()
    {
        var a = Path.Combine(_tempRoot, "A.csproj");
        var b = Path.Combine(_tempRoot, "B.csproj");
        await File.WriteAllTextAsync(a, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        await File.WriteAllTextAsync(b, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        var repos = new List<Repo> { new() { Name = "R", Url = "u", Branch = "main" } };
        var pb = new ProjectInfo { Name = "B", FilePath = b, PackageReferences = [], ProjectReferences = [] };
        var pa = new ProjectInfo
        {
            Name = "A",
            FilePath = a,
            PackageReferences = [],
            ProjectReferences = [b],
        };

        var fakeConfig = new FakeAppConfiguration
        {
            Next = new ConfigurationLoadResult(new AppConfig { Repos = repos }, Warning: null),
        };
        var fakeParse = new FakeParsingService
        {
            ProjectsByRepoName = new Dictionary<string, IReadOnlyList<ProjectInfo>>(StringComparer.OrdinalIgnoreCase)
            {
                ["R"] = [pa, pb],
            },
        };

        var sut = CreateGraphService(fakeConfig, fakeParse);
        await sut.BuildGraphAsync();

        var text = sut.FormatGraphText();

        Assert.Contains("->", text, StringComparison.Ordinal);
        Assert.Contains("A", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MakeNodeId_ReplacesSlashesInRepoAndProjectNames()
    {
        var id = GraphService.MakeNodeId("org/repo", "A/B");

        Assert.Equal("org_repo/A_B", id);
    }

    [Fact]
    public void MakeNodeId_WhenSegmentIsWhitespace_UsesUnderscorePlaceholder()
    {
        Assert.Equal("_/_", GraphService.MakeNodeId("   ", "  "));
    }

    [Fact]
    public async Task GetNode_WhenShortNameIsAmbiguous_ReturnsNull()
    {
        var x1 = Path.Combine(_tempRoot, "r1", "Lib.csproj");
        var x2 = Path.Combine(_tempRoot, "r2", "Lib.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(x1)!);
        Directory.CreateDirectory(Path.GetDirectoryName(x2)!);
        await File.WriteAllTextAsync(x1, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        await File.WriteAllTextAsync(x2, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        var repos = new List<Repo>
        {
            new() { Name = "RepoOne", Url = "u", Branch = "main" },
            new() { Name = "RepoTwo", Url = "u", Branch = "main" },
        };
        var p1 = new ProjectInfo { Name = "Lib", FilePath = x1, PackageReferences = [], ProjectReferences = [] };
        var p2 = new ProjectInfo { Name = "Lib", FilePath = x2, PackageReferences = [], ProjectReferences = [] };

        var fakeConfig = new FakeAppConfiguration
        {
            Next = new ConfigurationLoadResult(new AppConfig { Repos = repos }, Warning: null),
        };
        var fakeParse = new FakeParsingService
        {
            ProjectsByRepoName = new Dictionary<string, IReadOnlyList<ProjectInfo>>(StringComparer.OrdinalIgnoreCase)
            {
                ["RepoOne"] = [p1],
                ["RepoTwo"] = [p2],
            },
        };

        var sut = CreateGraphService(fakeConfig, fakeParse);
        await sut.BuildGraphAsync();

        Assert.Null(sut.GetNode("Lib"));
    }
}
