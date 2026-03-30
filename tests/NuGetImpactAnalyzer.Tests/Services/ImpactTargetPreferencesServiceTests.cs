using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class ImpactTargetPreferencesServiceTests
{
    private sealed class MemoryUserPreferences : IUserPreferencesService
    {
        public UserPreferences Stored { get; set; } = new();

        public UserPreferences Load() => Stored;

        public void Save(UserPreferences preferences) => Stored = preferences;
    }

    [Fact]
    public void LoadLastTargetPackage_ReturnsNullWhenEmpty()
    {
        var inner = new MemoryUserPreferences();
        var sut = new ImpactTargetPreferencesService(inner);

        Assert.Null(sut.LoadLastTargetPackage());
    }

    [Fact]
    public void LoadLastTargetPackage_TrimsAndReturnsValue()
    {
        var inner = new MemoryUserPreferences
        {
            Stored = new UserPreferences { LastTargetPackage = "  X  " },
        };
        var sut = new ImpactTargetPreferencesService(inner);

        Assert.Equal("X", sut.LoadLastTargetPackage());
    }

    [Fact]
    public void SaveLastTargetPackage_PersistsTrimmed()
    {
        var inner = new MemoryUserPreferences();
        var sut = new ImpactTargetPreferencesService(inner);

        sut.SaveLastTargetPackage("  A  ");

        Assert.Equal("A", inner.Stored.LastTargetPackage);
    }
}
