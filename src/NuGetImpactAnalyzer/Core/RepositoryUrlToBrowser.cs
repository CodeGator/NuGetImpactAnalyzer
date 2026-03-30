using System.Diagnostics.CodeAnalysis;

namespace NuGetImpactAnalyzer.Core;

/// <summary>
/// Maps a Git remote URL to an https URL suitable for opening in a browser (Git host web UI).
/// </summary>
public static class RepositoryUrlToBrowser
{
    /// <summary>
    /// Produces an https:// URL when possible (http(s) remotes, SCP, and common ssh:// forms).
    /// </summary>
    public static bool TryGetBrowserUrl(string? remoteUrl, [NotNullWhen(true)] out string? httpsUrl)
    {
        httpsUrl = null;
        if (string.IsNullOrWhiteSpace(remoteUrl))
        {
            return false;
        }

        var s = remoteUrl.Trim();

        if (Uri.TryCreate(s, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                httpsUrl = uri.ToString();
                return true;
            }

            if (string.Equals(uri.Scheme, "ssh", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(uri.Host))
            {
                var path = uri.AbsolutePath.Trim('/');
                if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase) && path.Length > 4)
                {
                    path = path[..^4];
                }

                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                httpsUrl = $"https://{uri.Host}/{path}";
                return true;
            }
        }

        // SCP: git@host:path (no "://")
        var at = s.IndexOf('@');
        var colon = s.IndexOf(':', StringComparison.Ordinal);
        if (at > 0 && colon > at && !s.Contains("://", StringComparison.Ordinal))
        {
            var host = s.AsSpan(at + 1, colon - at - 1).ToString();
            var path = s[(colon + 1)..].Trim().Trim('/');
            if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase) && path.Length > 4)
            {
                path = path[..^4];
            }

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(path))
            {
                return false;
            }

            httpsUrl = "https://" + host + "/" + path;
            return true;
        }

        return false;
    }
}
