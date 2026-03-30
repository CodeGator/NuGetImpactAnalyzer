using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

/// <summary>
/// File-backed parse cache under per-user local application data; thread-safe for concurrent UI and sync operations.
/// </summary>
public sealed class JsonParseResultCache : IParseResultCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _cacheFilePath;
    private readonly object _fileLock = new();

    public JsonParseResultCache()
        : this(Path.Combine(AppDataLocations.DefaultLocalDataRoot(), "cache.json"))
    {
    }

    public JsonParseResultCache(string cacheFilePath)
    {
        _cacheFilePath = cacheFilePath;
    }

    /// <inheritdoc />
    public bool TryGet(string repoName, string commitSha, [NotNullWhen(true)] out IReadOnlyList<ProjectInfo>? projects)
    {
        projects = null;
        if (string.IsNullOrWhiteSpace(repoName) || string.IsNullOrWhiteSpace(commitSha))
        {
            return false;
        }

        lock (_fileLock)
        {
            var doc = ReadDocument();
            var match = doc.Entries.FirstOrDefault(e =>
                e.RepoName.Equals(repoName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                e.CommitHash.Equals(commitSha, StringComparison.Ordinal));
            if (match is null)
            {
                return false;
            }

            projects = match.Projects;
            return true;
        }
    }

    /// <inheritdoc />
    public void Save(string repoName, string commitSha, IReadOnlyList<ProjectInfo> projects)
    {
        if (string.IsNullOrWhiteSpace(repoName) || string.IsNullOrWhiteSpace(commitSha))
        {
            return;
        }

        var name = repoName.Trim();
        var list = projects as List<ProjectInfo> ?? projects.ToList();

        lock (_fileLock)
        {
            var doc = ReadDocument();
            doc.Entries.RemoveAll(e => e.RepoName.Equals(name, StringComparison.OrdinalIgnoreCase));
            doc.Entries.Add(new ParseCacheRecord
            {
                RepoName = name,
                CommitHash = commitSha,
                Projects = list,
            });

            WriteDocument(doc);
        }
    }

    /// <inheritdoc />
    public void RemoveEntriesForRepository(string repoName)
    {
        if (string.IsNullOrWhiteSpace(repoName))
        {
            return;
        }

        var name = repoName.Trim();
        lock (_fileLock)
        {
            var doc = ReadDocument();
            var removed = doc.Entries.RemoveAll(e => e.RepoName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                WriteDocument(doc);
            }
        }
    }

    private ParseCacheDocument ReadDocument()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                return new ParseCacheDocument();
            }

            var json = File.ReadAllText(_cacheFilePath);
            var doc = JsonSerializer.Deserialize<ParseCacheDocument>(json, JsonOptions);
            return doc ?? new ParseCacheDocument();
        }
        catch (JsonException)
        {
            return new ParseCacheDocument();
        }
    }

    private void WriteDocument(ParseCacheDocument document)
    {
        var dir = Path.GetDirectoryName(_cacheFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(document, JsonOptions);
        var tempPath = _cacheFilePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _cacheFilePath, overwrite: true);
    }
}
