using System.IO;

namespace NuGetImpactAnalyzer.Core;

/// <summary>
/// Derives a short repository display name (folder key) from a Git remote URL.
/// </summary>
public static class RepositoryNameFromUrl
{
    /// <summary>
    /// Returns the last path segment, without <c>.git</c>, suitable for <see cref="Models.Repo.Name"/>.
    /// </summary>
    public static string Derive(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var s = url.Trim();

        // git@host:org/repo or git@host:repo.git (no "://")
        var at = s.IndexOf('@');
        var colon = s.IndexOf(':', StringComparison.Ordinal);
        if (at >= 0 && colon > at && !s.Contains("://", StringComparison.Ordinal))
        {
            var path = s[(colon + 1)..].Trim();
            return LastPathSegment(path);
        }

        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
        {
            if (!Uri.TryCreate("https://" + s.TrimStart('/'), UriKind.Absolute, out uri))
            {
                return SanitizeSegment(s);
            }
        }

        if (uri is null)
        {
            return string.Empty;
        }

        // file:///C:/path/to/repo
        if (uri.IsFile)
        {
            var p = uri.LocalPath.Replace('\\', '/').TrimEnd('/');
            return LastPathSegment(p);
        }

        var abs = uri.AbsolutePath.Trim('/');
        if (string.IsNullOrEmpty(abs))
        {
            return string.Empty;
        }

        return LastPathSegment(abs);
    }

    private static string LastPathSegment(string pathWithSlashes)
    {
        var parts = pathWithSlashes.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return string.Empty;
        }

        return StripGit(parts[^1]);
    }

    private static string StripGit(string segment)
    {
        var t = segment.Trim();
        if (t.EndsWith(".git", StringComparison.OrdinalIgnoreCase) && t.Length > 4)
        {
            t = t[..^4];
        }

        return SanitizeSegment(t);
    }

    private static string SanitizeSegment(string value)
    {
        var t = value.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            t = t.Replace(c, '_');
        }

        return t;
    }
}
