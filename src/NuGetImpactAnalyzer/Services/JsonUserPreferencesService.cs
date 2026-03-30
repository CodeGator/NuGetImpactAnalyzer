using System.IO;
using System.Text.Json;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

/// <summary>
/// JSON file under the per-user local application data folder.
/// </summary>
public sealed class JsonUserPreferencesService : IUserPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _path;
    private readonly object _lock = new();

    public JsonUserPreferencesService()
        : this(Path.Combine(AppDataLocations.DefaultLocalDataRoot(), "userpreferences.json"))
    {
    }

    public JsonUserPreferencesService(string filePath)
    {
        _path = filePath;
    }

    /// <inheritdoc />
    public UserPreferences Load()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_path))
                {
                    return new UserPreferences();
                }

                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<UserPreferences>(json, JsonOptions) ?? new UserPreferences();
            }
            catch (JsonException)
            {
                return new UserPreferences();
            }
        }
    }

    /// <inheritdoc />
    public void Save(UserPreferences preferences)
    {
        lock (_lock)
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(preferences, JsonOptions);
            var temp = _path + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, _path, overwrite: true);
        }
    }
}
