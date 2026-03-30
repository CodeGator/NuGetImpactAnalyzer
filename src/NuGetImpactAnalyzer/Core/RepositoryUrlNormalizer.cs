namespace NuGetImpactAnalyzer.Core;

/// <summary>
/// Canonical form for comparing Git remote URLs (HTTPS, SSH/SCP, file) when detecting duplicates.
/// </summary>
public static class RepositoryUrlNormalizer
{
    /// <summary>
    /// Returns a lowercase, path-normalized key, or empty if <paramref name="url"/> is unusable.
    /// </summary>
    public static string Normalize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var s = url.Trim();

        // SCP: git@host:path (no "://")
        var at = s.IndexOf('@');
        var colon = s.IndexOf(':', StringComparison.Ordinal);
        if (at > 0 && colon > at && !s.Contains("://", StringComparison.Ordinal))
        {
            var host = s.AsSpan(at + 1, colon - at - 1).ToString();
            var path = s[(colon + 1)..].Trim();
            return JoinHostPath(NormalizeHost(host), NormalizeRepoPath(path));
        }

        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
        {
            if (!Uri.TryCreate("https://" + s.TrimStart('/'), UriKind.Absolute, out uri))
            {
                return s.ToLowerInvariant();
            }
        }

        if (uri.IsFile)
        {
            var p = uri.LocalPath.Replace('\\', '/').TrimEnd('/');
            return "file:" + p.ToLowerInvariant();
        }

        var authority = uri.Host.ToLowerInvariant();
        if (!uri.IsDefaultPort)
        {
            authority += ":" + uri.Port;
        }

        var absPath = uri.AbsolutePath.Trim('/');
        return JoinHostPath(authority, NormalizeRepoPath(absPath));
    }

    /// <summary>True if both URLs refer to the same repository location (case-insensitive, .git, HTTPS vs SSH).</summary>
    public static bool AreSame(string? a, string? b) =>
        string.Equals(Normalize(a), Normalize(b), StringComparison.Ordinal);

    private static string JoinHostPath(string host, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return host;
        }

        return host + "/" + path;
    }

    private static string NormalizeHost(string host) => host.Trim().ToLowerInvariant();

    /// <summary>Strip trailing .git from last segment; lowercase segments.</summary>
    private static string NormalizeRepoPath(string path)
    {
        var p = path.Trim().Trim('/');
        if (string.IsNullOrEmpty(p))
        {
            return string.Empty;
        }

        var parts = p.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return string.Empty;
        }

        var last = parts[^1];
        if (last.EndsWith(".git", StringComparison.OrdinalIgnoreCase) && last.Length > 4)
        {
            parts[^1] = last[..^4];
        }

        return string.Join('/', parts.Select(x => x.ToLowerInvariant()));
    }
}
