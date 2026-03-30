using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class RepositoryProjectScopeTests : IDisposable
{
    private readonly string _root;

    public RepositoryProjectScopeTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "nuget-scope-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch
        {
            // best-effort
        }
    }

    [Fact]
    public void ApplyAnalysisScope_WhenPathEmpty_ReturnsAllProjects()
    {
        var repo = new Repo { AnalysisProjectRelativePath = "" };
        var a = new ProjectInfo
        {
            Name = "A",
            FilePath = Path.Combine(_root, "a.csproj"),
            PackageReferences = [],
            ProjectReferences = [],
        };

        var result = RepositoryProjectScope.ApplyAnalysisScope(repo, [a], _root);

        Assert.Single(result);
        Assert.Same(a, result[0]);
    }

    [Fact]
    public void ApplyAnalysisScope_WhenRootMatches_KeepsTransitiveRefsAndDropsUnrelated()
    {
        var appDir = Path.Combine(_root, "app");
        var libDir = Path.Combine(_root, "lib");
        var otherDir = Path.Combine(_root, "other");
        Directory.CreateDirectory(appDir);
        Directory.CreateDirectory(libDir);
        Directory.CreateDirectory(otherDir);
        var appCs = Path.Combine(appDir, "App.csproj");
        var libCs = Path.Combine(libDir, "Lib.csproj");
        var otherCs = Path.Combine(otherDir, "Other.csproj");
        File.WriteAllText(appCs, "");
        File.WriteAllText(libCs, "");
        File.WriteAllText(otherCs, "");

        var repo = new Repo { AnalysisProjectRelativePath = "app/App.csproj" };
        var other = new ProjectInfo
        {
            Name = "Other",
            FilePath = otherCs,
            PackageReferences = [],
            ProjectReferences = [],
        };
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
            ProjectReferences = [Path.Combine("..", "lib", "Lib.csproj")],
        };

        var result = RepositoryProjectScope.ApplyAnalysisScope(repo, [app, lib, other], _root)
            .Select(p => p.Name)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["App", "Lib"], result);
    }

    [Fact]
    public void ApplyAnalysisScope_WhenLocalRootMissing_ReturnsEmpty()
    {
        var repo = new Repo { AnalysisProjectRelativePath = "a.csproj" };
        var a = new ProjectInfo
        {
            Name = "A",
            FilePath = Path.Combine(_root, "a.csproj"),
            PackageReferences = [],
            ProjectReferences = [],
        };

        var result = RepositoryProjectScope.ApplyAnalysisScope(repo, [a], Path.Combine(_root, "missing"));

        Assert.Empty(result);
    }
}
