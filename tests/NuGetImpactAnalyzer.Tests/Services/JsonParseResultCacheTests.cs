using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class JsonParseResultCacheTests : IDisposable
{
    private readonly string _cachePath;
    private readonly List<string> _cleanup = [];

    public JsonParseResultCacheTests()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nuget-parse-cache-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _cachePath = Path.Combine(dir, "cache.json");
        _cleanup.Add(dir);
    }

    public void Dispose()
    {
        foreach (var p in _cleanup)
        {
            try
            {
                if (Directory.Exists(p))
                {
                    Directory.Delete(p, true);
                }
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void TryGet_WhenMissingFile_ReturnsFalse()
    {
        var sut = new JsonParseResultCache(_cachePath);

        var hit = sut.TryGet("Repo", "deadbeef", out var projects);

        Assert.False(hit);
        Assert.Null(projects);
    }

    [Fact]
    public void Save_ThenTryGet_RoundTripsProjects()
    {
        var sut = new JsonParseResultCache(_cachePath);
        var projects = new List<ProjectInfo>
        {
            new()
            {
                Name = "App",
                FilePath = @"C:\x\App.csproj",
                PackageVersion = "2.0.0",
                PackageReferences = [new PackageReferenceInfo { Include = "Newtonsoft.Json", Version = "13.0.1" }],
                ProjectReferences = [],
            },
        };

        sut.Save("MyRepo", "abc123def456", projects);

        var hit = sut.TryGet("MyRepo", "abc123def456", out var loaded);
        Assert.True(hit);
        Assert.NotNull(loaded);
        Assert.Single(loaded!);
        Assert.Equal("App", loaded[0].Name);
        Assert.Equal(@"C:\x\App.csproj", loaded[0].FilePath);
        Assert.Equal("2.0.0", loaded[0].PackageVersion);
        Assert.Single(loaded[0].PackageReferences);
        Assert.Equal("Newtonsoft.Json", loaded[0].PackageReferences[0].Include);
    }

    [Fact]
    public void Save_ReplacesEntryForSameRepoName()
    {
        var sut = new JsonParseResultCache(_cachePath);
        sut.Save("R", "sha1", [new ProjectInfo { Name = "A", FilePath = "/a", PackageReferences = [], ProjectReferences = [] }]);
        sut.Save("R", "sha2", [new ProjectInfo { Name = "B", FilePath = "/b", PackageReferences = [], ProjectReferences = [] }]);

        Assert.False(sut.TryGet("R", "sha1", out _));
        Assert.True(sut.TryGet("R", "sha2", out var list));
        Assert.Single(list!);
        Assert.Equal("B", list![0].Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void TryGet_WhenRepoNameOrCommitShaIsBlank_ReturnsFalse(string blank)
    {
        var sut = new JsonParseResultCache(_cachePath);

        Assert.False(sut.TryGet(blank, "deadbeef", out _));
        Assert.False(sut.TryGet("Repo", blank, out _));
    }

    [Fact]
    public void Save_WhenRepoNameOrCommitShaBlank_DoesNotWriteEntry()
    {
        var sut = new JsonParseResultCache(_cachePath);
        var project = new ProjectInfo { Name = "A", FilePath = "/a", PackageReferences = [], ProjectReferences = [] };

        sut.Save("", "sha", [project]);
        sut.Save("   ", "sha", [project]);
        sut.Save("R", "", [project]);
        sut.Save("R", "   ", [project]);

        Assert.False(sut.TryGet("R", "sha", out _));
        Assert.False(File.Exists(_cachePath));
    }

    [Fact]
    public void Save_WhenKeysBlank_AfterValidSave_DoesNotRemoveExistingEntry()
    {
        var sut = new JsonParseResultCache(_cachePath);
        sut.Save("Keep", "abc", [new ProjectInfo { Name = "K", FilePath = "/k", PackageReferences = [], ProjectReferences = [] }]);

        sut.Save("", "abc", [new ProjectInfo { Name = "X", FilePath = "/x", PackageReferences = [], ProjectReferences = [] }]);

        Assert.True(sut.TryGet("Keep", "abc", out var list));
        Assert.NotNull(list);
        Assert.Equal("K", list![0].Name);
    }

    [Fact]
    public void TryGet_WhenJsonFileIsCorrupt_ReturnsFalse()
    {
        File.WriteAllText(_cachePath, "{ not json");

        var sut = new JsonParseResultCache(_cachePath);

        Assert.False(sut.TryGet("R", "sha", out _));
    }

    [Fact]
    public void TryGet_RepoNameMatchIsCaseInsensitive()
    {
        var sut = new JsonParseResultCache(_cachePath);
        sut.Save("MyRepository", "deadbeef", [new ProjectInfo { Name = "P", FilePath = "/p", PackageReferences = [], ProjectReferences = [] }]);

        Assert.True(sut.TryGet("myrepository", "deadbeef", out var a));
        Assert.True(sut.TryGet("MYREPOSITORY", "deadbeef", out var b));
        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.Equal("P", a![0].Name);
    }

    [Fact]
    public void TryGet_CommitHashMustMatchExactly()
    {
        var sut = new JsonParseResultCache(_cachePath);
        sut.Save("R", "AbCdEf", [new ProjectInfo { Name = "P", FilePath = "/p", PackageReferences = [], ProjectReferences = [] }]);

        Assert.False(sut.TryGet("R", "abcdef", out _));
        Assert.True(sut.TryGet("R", "AbCdEf", out var hit));
        Assert.Equal("P", hit![0].Name);
    }

    [Fact]
    public void RemoveEntriesForRepository_RemovesEntry_CaseInsensitiveOnRepoName()
    {
        var sut = new JsonParseResultCache(_cachePath);
        var p = new ProjectInfo { Name = "P", FilePath = "/p", PackageReferences = [], ProjectReferences = [] };
        sut.Save("Keep", "a", [p]);
        sut.Save("DropMe", "deadbeef", [p]);

        sut.RemoveEntriesForRepository("dropme");

        Assert.True(sut.TryGet("Keep", "a", out _));
        Assert.False(sut.TryGet("DropMe", "deadbeef", out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RemoveEntriesForRepository_WhenNameBlank_IsNoOp(string blank)
    {
        var sut = new JsonParseResultCache(_cachePath);
        sut.Save("R", "sha", [new ProjectInfo { Name = "P", FilePath = "/p", PackageReferences = [], ProjectReferences = [] }]);

        sut.RemoveEntriesForRepository(blank);

        Assert.True(sut.TryGet("R", "sha", out _));
    }
}
