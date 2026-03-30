using System.IO;
using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services;

/// <summary>
/// Limits parsed projects to the transitive closure of a single root .csproj when <see cref="Repo.AnalysisProjectRelativePath"/> is set.
/// </summary>
public static class RepositoryProjectScope
{
    /// <summary>
    /// When <paramref name="repo"/>.AnalysisProjectRelativePath is empty, returns <paramref name="projects"/> unchanged.
    /// Otherwise returns projects reachable from that root via <see cref="ProjectInfo.ProjectReferences"/> within the same repository.
    /// </summary>
    public static IReadOnlyList<ProjectInfo> ApplyAnalysisScope(
        Repo repo,
        IReadOnlyList<ProjectInfo> projects,
        string localRepositoryRoot)
    {
        if (string.IsNullOrWhiteSpace(repo.AnalysisProjectRelativePath))
        {
            return projects is List<ProjectInfo> lp ? lp : [.. projects];
        }

        if (projects.Count == 0 || string.IsNullOrWhiteSpace(localRepositoryRoot) || !Directory.Exists(localRepositoryRoot))
        {
            return [];
        }

        var rootRelative = repo.AnalysisProjectRelativePath.Trim().Replace('/', Path.DirectorySeparatorChar);
        var rootPath = Path.GetFullPath(Path.Combine(localRepositoryRoot, rootRelative));
        var byPath = new Dictionary<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in projects)
        {
            byPath[Path.GetFullPath(p.FilePath)] = p;
        }

        if (!byPath.TryGetValue(rootPath, out _))
        {
            return [];
        }

        var inRepo = new HashSet<string>(byPath.Keys, StringComparer.OrdinalIgnoreCase);
        var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        queue.Enqueue(rootPath);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!included.Add(current))
            {
                continue;
            }

            if (!byPath.TryGetValue(current, out var proj))
            {
                continue;
            }

            var baseDir = Path.GetDirectoryName(proj.FilePath);
            if (string.IsNullOrEmpty(baseDir))
            {
                continue;
            }

            foreach (var rel in proj.ProjectReferences)
            {
                try
                {
                    var target = Path.GetFullPath(Path.Combine(baseDir, rel));
                    if (inRepo.Contains(target) && !included.Contains(target))
                    {
                        queue.Enqueue(target);
                    }
                }
                catch
                {
                    // ignore bad reference paths
                }
            }
        }

        return projects.Where(p => included.Contains(Path.GetFullPath(p.FilePath))).ToList();
    }
}
