using System.Net;
using System.Net.Http;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class GitHubRepositoryMetadataServiceValidationTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;

        public List<string> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(StatusCode));
        }
    }

    [Fact]
    public async Task ValidateGitHubRepositoryUrlAsync_NonGitHubUrl_ReturnsInvalidWithoutHttp()
    {
        var handler = new StubHandler();
        var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        var sut = new GitHubRepositoryMetadataService(new NoOpCredentialService(), http);

        var result = await sut.ValidateGitHubRepositoryUrlAsync("https://gitlab.com/a/b", "key", CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains("Only GitHub", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Empty(handler.RequestUris);
    }

    [Fact]
    public async Task ValidateGitHubRepositoryUrlAsync_HttpsOk_ReturnsValid()
    {
        var handler = new StubHandler { StatusCode = HttpStatusCode.OK };
        var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        var sut = new GitHubRepositoryMetadataService(new NoOpCredentialService(), http);

        var result = await sut.ValidateGitHubRepositoryUrlAsync(
            "https://github.com/microsoft/vscode",
            "key",
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Single(handler.RequestUris);
        Assert.Contains("api.github.com/repos/microsoft/vscode", handler.RequestUris[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidateGitHubRepositoryUrlAsync_NotFound_ReturnsInvalid()
    {
        var handler = new StubHandler { StatusCode = HttpStatusCode.NotFound };
        var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        var sut = new GitHubRepositoryMetadataService(new NoOpCredentialService(), http);

        var result = await sut.ValidateGitHubRepositoryUrlAsync(
            "https://github.com/nope/missing-repo-xyz",
            "key",
            CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateGitHubRepositoryUrlAsync_PublicRepo_DoesNotSendPATEvenWhenStored()
    {
        var handler = new RecordsAuthHandler();
        var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        var sut = new GitHubRepositoryMetadataService(new TokenCredentialService("ghp_stored"), http);

        var result = await sut.ValidateGitHubRepositoryUrlAsync(
            "https://github.com/CodeGator/CodeGator",
            "CodeGator",
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Single(handler.AuthorizationSchemes);
        Assert.Null(handler.AuthorizationSchemes[0]);
    }

    [Fact]
    public async Task ValidateGitHubRepositoryUrlAsync_NotFoundAnonymous_ThenOkWithToken_Valid()
    {
        var handler = new NotFoundThenOkHandler();
        var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        var sut = new GitHubRepositoryMetadataService(new TokenCredentialService("ghp_secret"), http);

        var result = await sut.ValidateGitHubRepositoryUrlAsync(
            "https://github.com/org/private-repo",
            "key",
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Equal(2, handler.RequestCount);
        Assert.Null(handler.AuthSchemes[0]);
        Assert.Equal("token", handler.AuthSchemes[1]);
    }

    private sealed class RecordsAuthHandler : HttpMessageHandler
    {
        public List<string?> AuthorizationSchemes { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthorizationSchemes.Add(request.Headers.Authorization?.Scheme);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class NotFoundThenOkHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        public List<string?> AuthSchemes { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            AuthSchemes.Add(request.Headers.Authorization?.Scheme);
            return Task.FromResult(
                RequestCount == 1
                    ? new HttpResponseMessage(HttpStatusCode.NotFound)
                    : new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class TokenCredentialService : ICredentialService
    {
        private readonly string _token;

        public TokenCredentialService(string token) => _token = token;

        public string? GetToken(string repoName) => _token;

        public void SaveToken(string repoName, string token) => throw new NotSupportedException();

        public void DeleteToken(string repoName) => throw new NotSupportedException();
    }

    private sealed class NoOpCredentialService : ICredentialService
    {
        public void DeleteToken(string repoName)
        {
        }

        public string? GetToken(string repoName) => null;

        public void SaveToken(string repoName, string token)
        {
        }
    }
}
