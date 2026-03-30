using System.IO;
using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

public sealed class GraphService : IGraphService
{
    /// <inheritdoc />
    public event EventHandler? GraphChanged;

    private readonly IAppConfigurationService _appConfiguration;
    private readonly IParsingService _parsingService;
    private readonly IGitService _gitService;

    private Dictionary<string, GraphNode> _nodes = new(StringComparer.OrdinalIgnoreCase);

    public GraphService(
        IAppConfigurationService appConfiguration,
        IParsingService parsingService,
        IGitService gitService)
    {
        _appConfiguration = appConfiguration;
        _parsingService = parsingService;
        _gitService = gitService;
    }

    public IReadOnlyDictionary<string, GraphNode> Nodes => _nodes;

    /// <inheritdoc />
    public void ClearGraph()
    {
        _nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase);
        GraphChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public async Task BuildGraphAsync(CancellationToken cancellationToken = default)
    {
        var config = _appConfiguration.Load().Config;
        var allProjects = new List<(string RepoName, ProjectInfo Project)>();

        foreach (var repo in config.Repos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var projects = await _parsingService.AnalyzeRepositoryAsync(repo, cancellationToken).ConfigureAwait(false);
            var localRoot = _gitService.GetLocalRepositoryPath(repo);
            var scoped = RepositoryProjectScope.ApplyAnalysisScope(repo, projects, localRoot);
            foreach (var p in scoped)
            {
                allProjects.Add((repo.Name, p));
            }
        }

        allProjects = allProjects
            .Where(pair => !DependencyGraphProjectFilter.ShouldExclude(pair.Project.Name))
            .ToList();

        var pathToNodeId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (repoName, p) in allProjects)
        {
            var id = MakeNodeId(repoName, p.Name);
            try
            {
                pathToNodeId[Path.GetFullPath(p.FilePath)] = id;
            }
            catch
            {
                // skip paths that cannot be normalized
            }
        }

        var newNodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var (repoName, p) in allProjects)
        {
            var id = MakeNodeId(repoName, p.Name);
            var deps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var packageConstraints = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            var baseDir = Path.GetDirectoryName(p.FilePath);
            if (!string.IsNullOrEmpty(baseDir))
            {
                foreach (var rel in p.ProjectReferences)
                {
                    try
                    {
                        var targetPath = Path.GetFullPath(Path.Combine(baseDir, rel));
                        if (pathToNodeId.TryGetValue(targetPath, out var targetId))
                        {
                            deps.Add(targetId);
                        }
                    }
                    catch
                    {
                        // ignore bad paths
                    }
                }
            }

            foreach (var pkg in p.PackageReferences)
            {
                var inc = pkg.Include.Trim();
                if (string.IsNullOrEmpty(inc))
                {
                    continue;
                }

                foreach (var (otherRepo, other) in allProjects)
                {
                    if (other.Name.Equals(inc, StringComparison.OrdinalIgnoreCase))
                    {
                        var targetId = MakeNodeId(otherRepo, other.Name);
                        deps.Add(targetId);
                        packageConstraints[targetId] = pkg.Version;
                    }
                }
            }

            newNodes[id] = new GraphNode
            {
                Name = p.Name,
                RepoName = repoName,
                Dependencies = deps.OrderBy(d => d, StringComparer.OrdinalIgnoreCase).ToList(),
                ResolvedPackageVersion = p.PackageVersion,
                PackageDependencyConstraints = packageConstraints,
            };
        }

        _nodes = newNodes;
        GraphChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public GraphNode? GetNode(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();

        if (_nodes.TryGetValue(trimmed, out var direct))
        {
            return direct;
        }

        var byShortName = _nodes.Values.Where(n => n.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase)).ToList();
        if (byShortName.Count == 1)
        {
            return byShortName[0];
        }

        if (byShortName.Count > 1)
        {
            return null;
        }

        var suffixKeys = _nodes.Keys
            .Where(k =>
                k.EndsWith("/" + trimmed, StringComparison.OrdinalIgnoreCase) ||
                k.EndsWith("\\" + trimmed, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (suffixKeys.Count == 1 && _nodes.TryGetValue(suffixKeys[0], out var bySuffix))
        {
            return bySuffix;
        }

        return null;
    }

    /// <inheritdoc />
    public string FormatGraphText()
    {
        if (_nodes.Count == 0)
        {
            return "No nodes. Configure repos, sync clones, then build the graph.";
        }

        var lines = new List<string>();
        // "Root" nodes are those with no dependencies (they don't reference any other project in the graph).
        // Put those first, alphabetically by display label, then list the rest alphabetically by display label.
        var ordered = _nodes
            .Select(kv => (Id: kv.Key, Node: kv.Value, Display: GraphDisplayFormatter.FormatNodeId(kv.Key, _nodes)))
            .OrderBy(x => x.Node.Dependencies.Count == 0 ? 0 : 1)
            .ThenBy(x => x.Display, StringComparer.OrdinalIgnoreCase);

        foreach (var kv in ordered)
        {
            var node = kv.Node;
            var left = kv.Display;
            if (node.Dependencies.Count == 0)
            {
                lines.Add($"{left} ->");
            }
            else
            {
                var parts = node.Dependencies
                    .Select(d => GraphDisplayFormatter.FormatNodeId(d, _nodes))
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                lines.Add($"{left} -> {string.Join(", ", parts)}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Stable id: sanitized repo + project name.</summary>
    public static string MakeNodeId(string repoName, string projectName)
    {
        var r = SanitizeSegment(repoName);
        var p = SanitizeSegment(projectName);
        return $"{r}/{p}";
    }

    private static string SanitizeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "_";
        }

        return value.Trim().Replace('/', '_').Replace('\\', '_');
    }
}
