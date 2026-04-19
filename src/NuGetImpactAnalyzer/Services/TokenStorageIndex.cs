using System.IO;
using System.Text.Json;

namespace NuGetImpactAnalyzer.Services;

/// <summary>
/// Tracks repository names that have a stored HTTPS token so master-password changes can re-wrap payloads
/// without scanning Credential Manager.
/// </summary>
public sealed class TokenStorageIndex
{
    private readonly string _path;
    private readonly object _gate = new();
    private HashSet<string> _repos = new(StringComparer.OrdinalIgnoreCase);

    public TokenStorageIndex(string storageDirectory)
    {
        Directory.CreateDirectory(storageDirectory);
        _path = Path.Combine(storageDirectory, "token-index.json");
        Load();
    }

    public IReadOnlyCollection<string> GetAll()
    {
        lock (_gate)
        {
            return _repos.ToList();
        }
    }

    public void Add(string repoName)
    {
        if (string.IsNullOrWhiteSpace(repoName))
        {
            return;
        }

        lock (_gate)
        {
            if (_repos.Add(repoName.Trim()))
            {
                Persist();
            }
        }
    }

    public void Remove(string repoName)
    {
        if (string.IsNullOrWhiteSpace(repoName))
        {
            return;
        }

        lock (_gate)
        {
            if (_repos.Remove(repoName.Trim()))
            {
                Persist();
            }
        }
    }

    public void ClearAll()
    {
        lock (_gate)
        {
            _repos.Clear();
            Persist();
        }
    }

    private void Load()
    {
        if (!File.Exists(_path))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_path);
            var names = JsonSerializer.Deserialize<List<string>>(json);
            if (names is not null)
            {
                _repos = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
            }
        }
        catch (JsonException)
        {
            // leave empty; user can re-save tokens
        }
    }

    private void Persist()
    {
        var json = JsonSerializer.Serialize(_repos.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray(),
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}
