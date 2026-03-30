using System.IO;
using System.Xml.Linq;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

public sealed class ParsingService : IParsingService
{
    private readonly IGitService _gitService;

    public ParsingService(IGitService gitService)
    {
        _gitService = gitService;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProjectInfo>> AnalyzeRepositoryAsync(Repo repo, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => AnalyzeCore(repo, cancellationToken), cancellationToken);
    }

    private IReadOnlyList<ProjectInfo> AnalyzeCore(Repo repo, CancellationToken cancellationToken)
    {
        var root = _gitService.GetLocalRepositoryPath(repo);
        if (!Directory.Exists(root))
        {
            return [];
        }

        var repoRoot = Path.GetFullPath(root);
        var csprojPaths = Directory.EnumerateFiles(repoRoot, "*.csproj", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var results = new List<ProjectInfo>();
        foreach (var path in csprojPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var centralVersions = LoadCentralPackageVersionsForProject(path, repoRoot, cancellationToken);
            var info = TryParseProjectFile(path, centralVersions, cancellationToken);
            if (info is not null)
            {
                results.Add(info);
            }
        }

        return results;
    }

    /// <summary>
    /// Loads merged PackageVersion entries from Directory.Packages.props files on the path from
    /// repository root down to the project directory (deeper files override shallower ones).
    /// </summary>
    internal static Dictionary<string, string> LoadCentralPackageVersionsForProject(
        string csprojPath,
        string repoRoot,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        repoRoot = Path.GetFullPath(repoRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        foreach (var dir in GetAncestorChainRootToLeaf(repoRoot, Path.GetDirectoryName(csprojPath)!))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var propsPath = Path.Combine(dir, "Directory.Packages.props");
            if (File.Exists(propsPath))
            {
                MergePackageVersionsFromFile(propsPath, map);
            }
        }

        return map;
    }

    /// <summary>
    /// Yields directory paths from <paramref name="repoRoot"/> down to the folder containing the project (inclusive).
    /// </summary>
    internal static IEnumerable<string> GetAncestorChainRootToLeaf(string repoRoot, string projectDirectory)
    {
        repoRoot = Path.GetFullPath(repoRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        projectDirectory = Path.GetFullPath(projectDirectory);

        if (!projectDirectory.StartsWith(repoRoot, StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        var relative = Path.GetRelativePath(repoRoot, projectDirectory);
        if (relative is "." or { Length: 0 })
        {
            yield return repoRoot;
            yield break;
        }

        yield return repoRoot;
        var current = repoRoot;
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (string.IsNullOrEmpty(segment))
            {
                continue;
            }

            current = Path.Combine(current, segment);
            yield return current;
        }
    }

    internal static void MergePackageVersionsFromFile(string propsPath, IDictionary<string, string> target)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Load(propsPath, LoadOptions.None);
        }
        catch
        {
            return;
        }

        var root = doc.Root;
        if (root is null)
        {
            return;
        }

        foreach (var el in root.Descendants())
        {
            if (!el.Name.LocalName.Equals("PackageVersion", StringComparison.Ordinal))
            {
                continue;
            }

            var include = el.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include))
            {
                continue;
            }

            var version = el.Attribute("Version")?.Value
                ?? el.Elements().FirstOrDefault(c =>
                    c.Name.LocalName.Equals("Version", StringComparison.Ordinal))?.Value;

            if (string.IsNullOrWhiteSpace(version))
            {
                continue;
            }

            target[include.Trim()] = version.Trim();
        }
    }

    private static ProjectInfo? TryParseProjectFile(
        string path,
        IReadOnlyDictionary<string, string> centralPackageVersions,
        CancellationToken cancellationToken)
    {
        try
        {
            var doc = XDocument.Load(path, LoadOptions.None);
            var root = doc.Root;
            if (root is null || !root.Name.LocalName.Equals("Project", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var projectName = ResolveProjectName(root, path);
            var packageVersion = TryReadProjectPackageVersion(root);
            var packages = new List<PackageReferenceInfo>();
            var projectRefs = new List<string>();

            foreach (var el in root.Descendants())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var local = el.Name.LocalName;
                if (local.Equals("PackageReference", StringComparison.Ordinal))
                {
                    var include = el.Attribute("Include")?.Value;
                    if (string.IsNullOrWhiteSpace(include))
                    {
                        continue;
                    }

                    var includeTrim = include.Trim();
                    var version = el.Attribute("Version")?.Value
                        ?? el.Elements().FirstOrDefault(c =>
                            c.Name.LocalName.Equals("Version", StringComparison.Ordinal))?.Value;

                    version = string.IsNullOrWhiteSpace(version) ? null : version.Trim();

                    if (version is null && centralPackageVersions.TryGetValue(includeTrim, out var centralVer))
                    {
                        version = centralVer;
                    }

                    packages.Add(new PackageReferenceInfo
                    {
                        Include = includeTrim,
                        Version = version,
                    });
                }
                else if (local.Equals("ProjectReference", StringComparison.Ordinal))
                {
                    var include = el.Attribute("Include")?.Value;
                    if (!string.IsNullOrWhiteSpace(include))
                    {
                        projectRefs.Add(include.Trim());
                    }
                }
            }

            return new ProjectInfo
            {
                Name = projectName,
                FilePath = path,
                PackageVersion = packageVersion,
                PackageReferences = packages,
                ProjectReferences = projectRefs,
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadProjectPackageVersion(XElement projectRoot)
    {
        foreach (var el in projectRoot.Elements())
        {
            if (!el.Name.LocalName.Equals("PropertyGroup", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var name in new[] { "Version", "PackageVersion" })
            {
                var verEl = el.Elements()
                    .FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.Ordinal));
                var v = verEl?.Value.Trim();
                if (!string.IsNullOrEmpty(v))
                {
                    return v;
                }
            }
        }

        return null;
    }

    private static string ResolveProjectName(XElement projectRoot, string filePath)
    {
        foreach (var candidate in new[] { "PackageId", "AssemblyName", "RootNamespace" })
        {
            var value = projectRoot.Descendants()
                .Where(e => e.Name.LocalName.Equals(candidate, StringComparison.Ordinal))
                .Select(e => e.Value.Trim())
                .FirstOrDefault(s => !string.IsNullOrEmpty(s));
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        return Path.GetFileNameWithoutExtension(filePath);
    }
}
