using System.Text.RegularExpressions;

namespace NuGetImpactAnalyzer.Services;

/// <summary>
/// Extracts GitHub owner and repository name from common clone URL shapes.
/// </summary>
public static partial class GitHubRepoUrlParser
{
    [GeneratedRegex(@"^git@github\.com[:/](?<owner>[^/]+)/(?<repo>.+?)(?:\.git)?$", RegexOptions.IgnoreCase)]
    private static partial Regex GitSshScpForm();

    /// <summary>
    /// Returns true if <paramref name="url"/> targets a github.com repository; output segments exclude <c>.git</c>.
    /// </summary>
    public static bool TryParseGitHubRepository(string? url, out string owner, out string repository)
    {
        owner = string.Empty;
        repository = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmed = url.Trim();

        if (trimmed.StartsWith("ssh://git@github.com/", StringComparison.OrdinalIgnoreCase))
        {
            var path = trimmed["ssh://git@github.com/".Length..];
            return TrySplitOwnerRepo(path, out owner, out repository);
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            return TrySplitOwnerRepo(uri.AbsolutePath, out owner, out repository);
        }

        var m = GitSshScpForm().Match(trimmed);
        if (!m.Success)
        {
            return false;
        }

        owner = m.Groups["owner"].Value;
        repository = StripGitSuffix(m.Groups["repo"].Value);
        return owner.Length > 0 && repository.Length > 0;
    }

    private static bool TrySplitOwnerRepo(string pathWithSegments, out string owner, out string repository)
    {
        owner = string.Empty;
        repository = string.Empty;
        var segments = pathWithSegments.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return false;
        }

        owner = segments[0];
        repository = StripGitSuffix(segments[1]);
        return owner.Length > 0 && repository.Length > 0;
    }

    private static string StripGitSuffix(string segment)
    {
        var s = segment.Trim();
        return s.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? s[..^4] : s;
    }
}
