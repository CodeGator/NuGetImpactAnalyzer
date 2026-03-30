using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class ParsingServiceDirectoryPackagesTests : IDisposable
{
    private readonly string _tempRoot;

    public ParsingServiceDirectoryPackagesTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "nuget-impact-dpm-test-" + Guid.NewGuid().ToString("N"));
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

    private sealed class FakeGitService : IGitService
    {
        public required string Root { get; init; }

        public string GetLocalRepositoryPath(Repo repo) => Root;

        public Task CloneOrUpdateAsync(Repo repo, IProgress<string>? progress = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SyncAllAsync(IEnumerable<Repo> repos, IProgress<string>? progress = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public string? TryGetHeadCommitSha(string localRepositoryPath) => null;

        public IReadOnlyList<string> ListBranches(Repo repo) => [];

        public IReadOnlyList<string> ListProjectRelativePaths(Repo repo) => [];

        public bool TryProbeRemoteRepository(Repo repo) => false;

        public bool TryDeleteLocalClone(Repo repo) => true;
    }

    [Fact]
    public async Task AnalyzeRepositoryAsync_ResolvesVersionFromDirectoryPackagesPropsWhenPackageReferenceOmitsVersion()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempRoot, "Directory.Packages.props"), """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Newtonsoft.Json" Version="13.0.1" />
              </ItemGroup>
            </Project>
            """);

        var appDir = Path.Combine(_tempRoot, "src", "App");
        Directory.CreateDirectory(appDir);
        var csproj = Path.Combine(appDir, "App.csproj");
        await File.WriteAllTextAsync(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>App</AssemblyName></PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" />
              </ItemGroup>
            </Project>
            """);

        var sut = new ParsingService(new FakeGitService { Root = _tempRoot });
        var projects = await sut.AnalyzeRepositoryAsync(new Repo { Name = "R", Url = "u", Branch = "main" });

        Assert.Single(projects);
        Assert.Single(projects[0].PackageReferences);
        Assert.Equal("Newtonsoft.Json", projects[0].PackageReferences[0].Include);
        Assert.Equal("13.0.1", projects[0].PackageReferences[0].Version);
    }

    [Fact]
    public async Task AnalyzeRepositoryAsync_DeepDirectoryPackagesPropsOverridesRoot()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempRoot, "Directory.Packages.props"), """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Newtonsoft.Json" Version="13.0.0" />
              </ItemGroup>
            </Project>
            """);

        var srcDir = Path.Combine(_tempRoot, "src");
        Directory.CreateDirectory(srcDir);
        await File.WriteAllTextAsync(Path.Combine(srcDir, "Directory.Packages.props"), """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Newtonsoft.Json" Version="13.0.2" />
              </ItemGroup>
            </Project>
            """);

        var appDir = Path.Combine(srcDir, "App");
        Directory.CreateDirectory(appDir);
        var csproj = Path.Combine(appDir, "App.csproj");
        await File.WriteAllTextAsync(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" />
              </ItemGroup>
            </Project>
            """);

        var sut = new ParsingService(new FakeGitService { Root = _tempRoot });
        var projects = await sut.AnalyzeRepositoryAsync(new Repo { Name = "R", Url = "u", Branch = "main" });

        Assert.Single(projects[0].PackageReferences);
        Assert.Equal("13.0.2", projects[0].PackageReferences[0].Version);
    }

    [Fact]
    public async Task AnalyzeRepositoryAsync_PackageReferenceVersionTakesPrecedenceOverCentral()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempRoot, "Directory.Packages.props"), """
            <Project>
              <ItemGroup>
                <PackageVersion Include="Newtonsoft.Json" Version="13.0.1" />
              </ItemGroup>
            </Project>
            """);

        var csproj = Path.Combine(_tempRoot, "Solo.csproj");
        await File.WriteAllTextAsync(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="9.0.1" />
              </ItemGroup>
            </Project>
            """);

        var sut = new ParsingService(new FakeGitService { Root = _tempRoot });
        var projects = await sut.AnalyzeRepositoryAsync(new Repo { Name = "R", Url = "u", Branch = "main" });

        Assert.Equal("9.0.1", projects[0].PackageReferences[0].Version);
    }

    [Fact]
    public void MergePackageVersionsFromFile_ReadsVersionChildElement()
    {
        var path = Path.Combine(_tempRoot, "Directory.Packages.props");
        File.WriteAllText(path, """
            <Project>
              <ItemGroup>
                <PackageVersion Include="X.Lib">
                  <Version>2.1.0</Version>
                </PackageVersion>
              </ItemGroup>
            </Project>
            """);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ParsingService.MergePackageVersionsFromFile(path, map);

        Assert.Equal("2.1.0", map["X.Lib"]);
    }

    [Fact]
    public void GetAncestorChainRootToLeaf_IncludesRootAndNestedFolders()
    {
        var repo = Path.Combine(_tempRoot, "repo");
        var leaf = Path.Combine(repo, "a", "b");
        Directory.CreateDirectory(leaf);

        var chain = ParsingService.GetAncestorChainRootToLeaf(repo, leaf).ToList();

        Assert.Equal(3, chain.Count);
        Assert.Equal(Path.GetFullPath(repo), Path.GetFullPath(chain[0]));
        Assert.Equal(Path.GetFullPath(Path.Combine(repo, "a")), Path.GetFullPath(chain[1]));
        Assert.Equal(Path.GetFullPath(leaf), Path.GetFullPath(chain[2]));
    }
}
