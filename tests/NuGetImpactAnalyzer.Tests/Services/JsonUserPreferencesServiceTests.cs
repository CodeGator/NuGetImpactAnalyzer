using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class JsonUserPreferencesServiceTests : IDisposable
{
    private readonly string _dir;
    private readonly string _path;

    public JsonUserPreferencesServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "nuget-user-prefs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _path = Path.Combine(_dir, "userpreferences.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public void Load_WhenMissing_ReturnsEmpty()
    {
        var sut = new JsonUserPreferencesService(_path);

        var prefs = sut.Load();

        Assert.Equal(string.Empty, prefs.LastTargetPackage);
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsLastTargetPackage()
    {
        var sut = new JsonUserPreferencesService(_path);

        sut.Save(new UserPreferences { LastTargetPackage = "Newtonsoft.Json" });

        var loaded = sut.Load();
        Assert.Equal("Newtonsoft.Json", loaded.LastTargetPackage);
    }

    [Fact]
    public void Load_WhenCorruptJson_ReturnsEmpty()
    {
        File.WriteAllText(_path, "{");
        var sut = new JsonUserPreferencesService(_path);

        var prefs = sut.Load();

        Assert.Equal(string.Empty, prefs.LastTargetPackage);
    }

    [Fact]
    public void Save_CreatesParentDirectoryWhenMissing()
    {
        var nestedDir = Path.Combine(_dir, "nested", "deep");
        var path = Path.Combine(nestedDir, "prefs.json");
        var sut = new JsonUserPreferencesService(path);

        sut.Save(new UserPreferences { LastTargetPackage = "X" });

        Assert.True(File.Exists(path));
        var loaded = sut.Load();
        Assert.Equal("X", loaded.LastTargetPackage);
    }

    [Fact]
    public void Load_WhenFileIsEmptyObject_ReturnsEmptyLastTarget()
    {
        File.WriteAllText(_path, "{}");
        var sut = new JsonUserPreferencesService(_path);

        var prefs = sut.Load();

        Assert.Equal(string.Empty, prefs.LastTargetPackage);
    }

    [Fact]
    public void Save_SecondSave_OverwritesFirst()
    {
        var sut = new JsonUserPreferencesService(_path);
        sut.Save(new UserPreferences { LastTargetPackage = "First" });
        sut.Save(new UserPreferences { LastTargetPackage = "Second" });

        Assert.Equal("Second", sut.Load().LastTargetPackage);
    }
}
