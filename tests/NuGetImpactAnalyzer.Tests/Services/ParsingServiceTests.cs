using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class ParsingServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public ParsingServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "nuget-impact-parse-test-" + Guid.NewGuid().ToString("N"));
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
    public async Task AnalyzeRepositoryAsync_ParsesPackageAndProjectReferences()
    {
        var sub = Path.Combine(_tempRoot, "src");
        Directory.CreateDirectory(sub);
        var csprojPath = Path.Combine(sub, "Sample.csproj");
        await File.WriteAllTextAsync(csprojPath, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <AssemblyName>Sample.App</AssemblyName>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
                <ProjectReference Include="..\\Other\\Other.csproj" />
              </ItemGroup>
            </Project>
            """);

        var git = new FakeGitService { Root = _tempRoot };
        var sut = new ParsingService(git);
        var repo = new Repo { Name = "T", Url = "https://x", Branch = "main" };

        var projects = await sut.AnalyzeRepositoryAsync(repo);

        Assert.Single(projects);
        var p = projects[0];
        Assert.Equal("Sample.App", p.Name);
        Assert.Equal(csprojPath, p.FilePath);
        Assert.Single(p.PackageReferences);
        Assert.Equal("Newtonsoft.Json", p.PackageReferences[0].Include);
        Assert.Equal("13.0.3", p.PackageReferences[0].Version);
        Assert.Single(p.ProjectReferences);
        Assert.Contains("Other", p.ProjectReferences[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeRepositoryAsync_FindsAllCsprojFilesSortedByPath()
    {
        var a = Path.Combine(_tempRoot, "A", "A.csproj");
        var b = Path.Combine(_tempRoot, "B", "B.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(a)!);
        Directory.CreateDirectory(Path.GetDirectoryName(b)!);
        await File.WriteAllTextAsync(a, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        await File.WriteAllTextAsync(b, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);

        var git = new FakeGitService { Root = _tempRoot };
        var sut = new ParsingService(git);
        var repo = new Repo { Name = "Multi", Url = "u", Branch = "main" };

        var projects = await sut.AnalyzeRepositoryAsync(repo);

        Assert.Equal(2, projects.Count);
        Assert.Equal(a, projects[0].FilePath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(b, projects[1].FilePath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("A", projects[0].Name);
        Assert.Equal("B", projects[1].Name);
    }

    [Fact]
    public async Task AnalyzeRepositoryAsync_WhenRootMissing_ReturnsEmpty()
    {
        var missing = Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("N"));
        var git = new FakeGitService { Root = missing };
        var sut = new ParsingService(git);
        var repo = new Repo { Name = "X", Url = "u", Branch = "main" };

        var projects = await sut.AnalyzeRepositoryAsync(repo);

        Assert.Empty(projects);
    }
}
