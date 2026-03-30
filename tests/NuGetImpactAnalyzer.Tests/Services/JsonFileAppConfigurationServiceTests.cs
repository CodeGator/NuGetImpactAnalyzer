using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class JsonFileAppConfigurationServiceTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var path in _tempFiles)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort cleanup for temp test files.
            }
        }
    }

    private string CreateTempConfigFile(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"nuget-impact-config-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        _tempFiles.Add(path);
        return path;
    }

    [Fact]
    public void Load_WhenFileMissing_ReturnsEmptyReposAndWarningWithPath()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"definitely-missing-{Guid.NewGuid():N}.json");
        var sut = new JsonFileAppConfigurationService(missingPath);

        var result = sut.Load();

        Assert.NotNull(result.Config);
        Assert.Empty(result.Config.Repos);
        Assert.NotNull(result.Warning);
        Assert.Contains(missingPath, result.Warning, StringComparison.Ordinal);
        Assert.Contains("not found", result.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_WhenDuplicateRepoNames_KeepsFirstOfEachAndWarns()
    {
        var json = """
            {
              "repos": [
                { "name": "CodeGator", "url": "https://example.com/1.git", "branch": "main" },
                { "name": "CodeGator", "url": "https://example.com/2.git", "branch": "main" },
                { "name": "CodeGator.Blazor", "url": "https://example.com/b.git", "branch": "main" },
                { "name": "Other", "url": "https://example.com/o.git", "branch": "develop" },
                { "name": "codegator", "url": "https://example.com/ci.git", "branch": "topic" }
              ]
            }
            """;
        var path = CreateTempConfigFile(json);
        var sut = new JsonFileAppConfigurationService(path);

        var result = sut.Load();

        Assert.NotNull(result.Warning);
        Assert.Contains("duplicate", result.Warning, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, result.Config.Repos.Count);
        Assert.Equal("CodeGator", result.Config.Repos[0].Name);
        Assert.Equal("https://example.com/1.git", result.Config.Repos[0].Url);
        Assert.Equal("CodeGator.Blazor", result.Config.Repos[1].Name);
        Assert.Equal("Other", result.Config.Repos[2].Name);
    }

    [Fact]
    public void Load_WhenFileValid_ReturnsReposAndNoWarning()
    {
        var json = """
            {
              "repos": [
                {
                  "name": "Alpha",
                  "url": "https://example.com/a.git",
                  "branch": "develop"
                },
                {
                  "name": "Beta",
                  "url": "https://example.com/b.git",
                  "branch": "main"
                }
              ]
            }
            """;
        var path = CreateTempConfigFile(json);
        var sut = new JsonFileAppConfigurationService(path);

        var result = sut.Load();

        Assert.Null(result.Warning);
        Assert.Equal(2, result.Config.Repos.Count);
        Assert.Equal("Alpha", result.Config.Repos[0].Name);
        Assert.Equal("https://example.com/a.git", result.Config.Repos[0].Url);
        Assert.Equal("develop", result.Config.Repos[0].Branch);
        Assert.Equal("Beta", result.Config.Repos[1].Name);
        Assert.Equal("main", result.Config.Repos[1].Branch);
    }

    [Fact]
    public void Load_WhenReposContainLegacyIsPrivate_IgnoresUnknownProperty()
    {
        var json = """
            {
              "repos": [
                {
                  "name": "Secret",
                  "url": "https://github.com/org/private.git",
                  "branch": "main",
                  "isPrivate": true
                },
                {
                  "name": "Open",
                  "url": "https://github.com/org/open.git",
                  "branch": "develop"
                }
              ]
            }
            """;
        var path = CreateTempConfigFile(json);
        var sut = new JsonFileAppConfigurationService(path);

        var result = sut.Load();

        Assert.Null(result.Warning);
        Assert.Equal(2, result.Config.Repos.Count);
        Assert.Equal("Secret", result.Config.Repos[0].Name);
        Assert.Equal("Open", result.Config.Repos[1].Name);
    }

    [Fact]
    public void Load_WhenReposEmpty_ReturnsEmptyListAndNoWarning()
    {
        var json = """{ "repos": [] }""";
        var path = CreateTempConfigFile(json);
        var sut = new JsonFileAppConfigurationService(path);

        var result = sut.Load();

        Assert.Null(result.Warning);
        Assert.Empty(result.Config.Repos);
    }

    [Fact]
    public void Load_WhenRootIsEmptyObject_ReturnsEmptyReposWithoutWarning()
    {
        var path = CreateTempConfigFile("{}");
        var sut = new JsonFileAppConfigurationService(path);

        var result = sut.Load();

        Assert.Null(result.Warning);
        Assert.NotNull(result.Config.Repos);
        Assert.Empty(result.Config.Repos);
    }

    [Fact]
    public void Load_WhenJsonMalformed_ReturnsEmptyReposAndWarning()
    {
        var path = CreateTempConfigFile("{ invalid");
        var sut = new JsonFileAppConfigurationService(path);

        var result = sut.Load();

        Assert.NotNull(result.Warning);
        Assert.Contains("Could not parse", result.Warning, StringComparison.Ordinal);
        Assert.Empty(result.Config.Repos);
    }

    [Fact]
    public void Load_WhenFileIsEmpty_ReturnsWarningAndEmptyRepos()
    {
        var path = CreateTempConfigFile("");
        var sut = new JsonFileAppConfigurationService(path);

        var result = sut.Load();

        Assert.NotNull(result.Warning);
        Assert.Contains("Could not parse", result.Warning, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Config.Repos);
    }

    [Fact]
    public void Load_WhenJsonHasCommentsAndTrailingComma_ParsesSuccessfully()
    {
        var json = """
            {
              // repository list
              "repos": [
                {
                  "name": "One",
                  "url": "https://example.com/1.git",
                  "branch": "main",
                },
              ],
            }
            """;
        var path = CreateTempConfigFile(json);
        var sut = new JsonFileAppConfigurationService(path);

        var result = sut.Load();

        Assert.Null(result.Warning);
        Assert.Single(result.Config.Repos);
        Assert.Equal("One", result.Config.Repos[0].Name);
    }

    [Fact]
    public void Load_WhenFileContainsJsonNullLiteral_ReturnsEmptyConfigWithoutWarning()
    {
        var path = CreateTempConfigFile("null");
        var sut = new JsonFileAppConfigurationService(path);

        var result = sut.Load();

        Assert.Null(result.Warning);
        Assert.NotNull(result.Config);
        Assert.Empty(result.Config.Repos);
    }

    [Fact]
    public void Load_WhenReposPropertyIsExplicitlyNull_ReturnsEmptyReposList()
    {
        var json = """{ "repos": null }""";
        var path = CreateTempConfigFile(json);
        var sut = new JsonFileAppConfigurationService(path);

        var result = sut.Load();

        Assert.Null(result.Warning);
        Assert.NotNull(result.Config.Repos);
        Assert.Empty(result.Config.Repos);
    }

    [Fact]
    public void Load_PropertyNamesAreCaseInsensitive()
    {
        var json = """
            {
              "REPOS": [
                {
                  "NAME": "Mixed",
                  "URL": "https://example.com/x.git",
                  "BRANCH": "topic"
                }
              ]
            }
            """;
        var path = CreateTempConfigFile(json);
        var sut = new JsonFileAppConfigurationService(path);

        var result = sut.Load();

        Assert.Null(result.Warning);
        Assert.Single(result.Config.Repos);
        Assert.Equal("Mixed", result.Config.Repos[0].Name);
        Assert.Equal("https://example.com/x.git", result.Config.Repos[0].Url);
        Assert.Equal("topic", result.Config.Repos[0].Branch);
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsRepositories()
    {
        var path = CreateTempConfigFile("{}");
        var sut = new JsonFileAppConfigurationService(path);
        var toSave = new AppConfig
        {
            Repos =
            [
                new Repo { Name = "Svc", Url = "https://github.com/org/repo.git", Branch = "develop" },
                new Repo { Name = "Other", Url = "https://a.git", Branch = "main" },
            ],
        };

        sut.Save(toSave);

        var result = sut.Load();

        Assert.Null(result.Warning);
        Assert.Equal(2, result.Config.Repos.Count);
        Assert.Equal("Svc", result.Config.Repos[0].Name);
        Assert.Equal("https://github.com/org/repo.git", result.Config.Repos[0].Url);
        Assert.Equal("develop", result.Config.Repos[0].Branch);
        Assert.Equal("Other", result.Config.Repos[1].Name);
    }

    [Fact]
    public void Save_WritesCamelCaseReposKey()
    {
        var path = CreateTempConfigFile("{}");
        var sut = new JsonFileAppConfigurationService(path);
        sut.Save(new AppConfig { Repos = [new Repo { Name = "One", Url = "https://1.git", Branch = "main" }] });

        var text = File.ReadAllText(path);

        Assert.Contains("\"repos\"", text, StringComparison.Ordinal);
        Assert.Contains("\"name\": \"One\"", text, StringComparison.Ordinal);
        Assert.Contains("\"url\": \"https://1.git\"", text, StringComparison.Ordinal);
        Assert.Contains("\"branch\": \"main\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Save_DoesNotPersistHasStoredCredentials()
    {
        var path = CreateTempConfigFile("{}");
        var sut = new JsonFileAppConfigurationService(path);
        var repo = new Repo { Name = "G", Url = "https://github.com/a/b.git", Branch = "main" };
        repo.HasStoredCredentials = true;

        sut.Save(new AppConfig { Repos = [repo] });

        var text = File.ReadAllText(path);
        Assert.DoesNotContain("hasStoredCredentials", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Save_DoesNotPersistGitHubIsPrivate()
    {
        var path = CreateTempConfigFile("{}");
        var sut = new JsonFileAppConfigurationService(path);
        var repo = new Repo { Name = "G", Url = "https://github.com/a/b.git", Branch = "main" };
        repo.GitHubIsPrivate = true;

        sut.Save(new AppConfig { Repos = [repo] });

        var text = File.ReadAllText(path);
        Assert.DoesNotContain("gitHubIsPrivate", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Save_ReplacesExistingFileContent()
    {
        var path = CreateTempConfigFile("""{"repos":[{"name":"Old","url":"u","branch":"main"}]}""");
        var sut = new JsonFileAppConfigurationService(path);

        sut.Save(new AppConfig { Repos = [new Repo { Name = "Fresh", Url = "https://x.git", Branch = "topic" }] });

        var result = sut.Load();
        Assert.Null(result.Warning);
        Assert.Single(result.Config.Repos);
        Assert.Equal("Fresh", result.Config.Repos[0].Name);
    }
}

