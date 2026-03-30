using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

/// <inheritdoc />
public sealed class GitHubRepositoryMetadataService : IGitHubRepositoryMetadataService
{
    private readonly ICredentialService _credentials;
    private readonly HttpClient _http;

    /// <summary>
    /// Creates the service using a shared, DI-managed <see cref="HttpClient"/>.
    /// </summary>
    public GitHubRepositoryMetadataService(ICredentialService credentials, HttpClient http)
    {
        _credentials = credentials;
        _http = http;
    }

    /// <inheritdoc />
    public async Task<GitHubRepositoryUrlValidationResult> ValidateGitHubRepositoryUrlAsync(
        string url,
        string credentialLookupName,
        CancellationToken cancellationToken = default)
    {
        if (!GitHubRepoUrlParser.TryParseGitHubRepository(url, out var owner, out var slug))
        {
            return new GitHubRepositoryUrlValidationResult(
                false,
                "Only GitHub repository URLs are supported. Use https://github.com/owner/repo or git@github.com:owner/repo.git.");
        }

        var token = _credentials.GetToken(credentialLookupName);

        try
        {
            using var response = await SendGitHubRepositoryGetAsync(owner, slug, token, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new GitHubRepositoryUrlValidationResult(
                    false,
                    "That GitHub repository was not found, or it is private and no token is stored (or the token cannot access it). Check the URL and use Manage credentials for private repositories.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new GitHubRepositoryUrlValidationResult(
                    false,
                    "GitHub denied access (403). For private repositories, ensure your personal access token is valid and has the 'repo' scope.");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new GitHubRepositoryUrlValidationResult(
                    false,
                    "GitHub rejected the stored token (401). Update credentials under Manage credentials.");
            }

            if ((int)response.StatusCode == 429)
            {
                return new GitHubRepositoryUrlValidationResult(
                    false,
                    "GitHub rate limit exceeded. Try again in a minute.");
            }

            if (response.IsSuccessStatusCode)
            {
                return new GitHubRepositoryUrlValidationResult(true);
            }

            return new GitHubRepositoryUrlValidationResult(
                false,
                $"GitHub returned {(int)response.StatusCode}. The repository could not be verified.");
        }
        catch (HttpRequestException ex)
        {
            return new GitHubRepositoryUrlValidationResult(false, $"Could not reach GitHub: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return new GitHubRepositoryUrlValidationResult(
                false,
                "The request to GitHub timed out. Check your network and try again.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(Repo Repo, bool? GitHubReportsPrivate)>> GetGitHubPrivateFlagsAsync(
        IReadOnlyList<Repo> repos,
        CancellationToken cancellationToken = default)
    {
        if (repos.Count == 0)
        {
            return [];
        }

        using var gate = new SemaphoreSlim(6, 6);
        var tasks = repos.Select(async repo =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!GitHubRepoUrlParser.TryParseGitHubRepository(repo.Url, out var owner, out var slug))
                {
                    return (repo, (bool?)null);
                }

                var value = await QueryPrivateAsync(owner, slug, repo.Name, cancellationToken).ConfigureAwait(false);
                return (repo, value);
            }
            finally
            {
                gate.Release();
            }
        });

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task<bool?> QueryPrivateAsync(
        string owner,
        string repositorySlug,
        string credentialLookupName,
        CancellationToken cancellationToken)
    {
        var token = _credentials.GetToken(credentialLookupName);

        try
        {
            using var response = await SendGitHubRepositoryGetAsync(owner, repositorySlug, token, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("private", out var priv)
                || priv.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
            {
                return null;
            }

            return priv.GetBoolean();
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// GET /repos/{owner}/{repo}. Calls GitHub <strong>without</strong> a PAT first so public repos never depend on stored credentials.
    /// Retries with a PAT only after 404 (typical for private repos when unauthenticated) or 401/403/429 (e.g. rate limit).
    /// </summary>
    private async Task<HttpResponseMessage> SendGitHubRepositoryGetAsync(
        string owner,
        string slug,
        string? token,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await SendOnce(null).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return response;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            response.Dispose();
            return await SendOnce(token).ConfigureAwait(false);
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized
            || response.StatusCode == HttpStatusCode.Forbidden
            || (int)response.StatusCode == 429)
        {
            response.Dispose();
            return await SendOnce(token).ConfigureAwait(false);
        }

        return response;

        async Task<HttpResponseMessage> SendOnce(string? t)
        {
            var path = $"{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(slug)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{path}");
            request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
            request.Headers.UserAgent.ParseAdd("NuGetImpactAnalyzer");
            ApplyGitHubRestApiAuthorization(request, t);
            return await _http
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Classic PATs (e.g. ghp_…) use <c>Authorization: token</c>; fine-grained and OAuth-style tokens use <c>Bearer</c>
    /// per GitHub REST guidance (only used when an authenticated retry is required).
    /// </summary>
    private static void ApplyGitHubRestApiAuthorization(HttpRequestMessage request, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var t = token.Trim();
        if (t.StartsWith("github_pat_", StringComparison.Ordinal)
            || t.StartsWith("gho_", StringComparison.Ordinal)
            || t.StartsWith("ghu_", StringComparison.Ordinal)
            || t.StartsWith("ghs_", StringComparison.Ordinal))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", t);
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("token", t);
    }
}
