using System.IO;
using System.Text.Json;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

/// <summary>
/// Reads <see cref="AppConfig"/> from a JSON file on disk.
/// </summary>
public sealed class JsonFileAppConfigurationService : IAppConfigurationService
{
    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _configPath;

    public JsonFileAppConfigurationService()
        : this(Path.Combine(AppDataLocations.DefaultLocalDataRoot(), "config.json"))
    {
    }

    public JsonFileAppConfigurationService(string configPath)
    {
        _configPath = configPath;
    }

    /// <inheritdoc />
    public ConfigurationLoadResult Load()
    {
        TrySeedUserConfigFromAppDirectory();

        if (!File.Exists(_configPath))
        {
            var missingWarning = $"config.json not found at \"{_configPath}\"; using an empty repository list.";
            return new ConfigurationLoadResult(new AppConfig(), missingWarning);
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonReadOptions) ?? new AppConfig();
            var repos = config.Repos ?? [];
            var deduped = DeduplicateReposByName(repos, out var skippedDuplicates);
            string? warning = null;
            if (skippedDuplicates > 0)
            {
                warning =
                    $"config.json contained {skippedDuplicates} duplicate repo entr{(skippedDuplicates == 1 ? "y" : "ies")} "
                    + "with the same name (after trimming); keeping the first of each. Save settings to rewrite the file.";
            }

            return new ConfigurationLoadResult(new AppConfig { Repos = deduped }, Warning: warning);
        }
        catch (JsonException ex)
        {
            var parseWarning = $"Could not parse config.json: {ex.Message}";
            return new ConfigurationLoadResult(new AppConfig(), parseWarning);
        }
    }

    /// <summary>
    /// First run: copy <c>config.json</c> from the app directory (optional template next to the executable) into per-user data.
    /// Runs only for the default user data path so unit tests (temp paths) and non-default DI roots are unaffected.
    /// </summary>
    private void TrySeedUserConfigFromAppDirectory()
    {
        if (File.Exists(_configPath))
        {
            return;
        }

        var defaultUserConfig = Path.Combine(AppDataLocations.DefaultLocalDataRoot(), "config.json");
        if (!string.Equals(Path.GetFullPath(_configPath), Path.GetFullPath(defaultUserConfig), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var bundled = Path.Combine(AppContext.BaseDirectory, "config.json");
        if (!File.Exists(bundled))
        {
            return;
        }

        try
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.Copy(bundled, _configPath, overwrite: false);
        }
        catch
        {
            // best-effort; missing-file handling below still applies
        }
    }

    /// <inheritdoc />
    public void Save(AppConfig config)
    {
        var toWrite = new AppConfig { Repos = [.. config.Repos] };
        var json = JsonSerializer.Serialize(toWrite, JsonWriteOptions);
        File.WriteAllText(_configPath, json);
    }

    /// <summary>
    /// Same display name implies the same on-disk clone folder; duplicates are usually accidental JSON duplication.
    /// </summary>
    private static List<Repo> DeduplicateReposByName(IReadOnlyList<Repo> repos, out int skippedDuplicates)
    {
        skippedDuplicates = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<Repo>(repos.Count);
        foreach (var repo in repos)
        {
            var key = RepoConfigDedupeKey(repo);
            if (!seen.Add(key))
            {
                skippedDuplicates++;
                continue;
            }

            list.Add(repo);
        }

        return list;
    }

    private static string RepoConfigDedupeKey(Repo repo)
    {
        var name = repo.Name?.Trim() ?? "";
        if (name.Length > 0)
        {
            return name;
        }

        var url = repo.Url?.Trim() ?? "";
        if (url.Length > 0)
        {
            return "\0" + url;
        }

        return "";
    }
}
