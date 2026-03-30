using System.Net;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class GitHubRepositoryMetadataServiceTests
{
    private sealed class StubCredentialService : ICredentialService
    {
        public string? Token { get; set; }

        public void SaveToken(string repoName, string token) => throw new NotSupportedException();

        public string? GetToken(string repoName) => Token;

        public void DeleteToken(string repoName) => throw new NotSupportedException();
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public Func<HttpRequestMessage, HttpResponseMessage>? Respond { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var r = Respond?.Invoke(request)
                    ?? new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"private\": false}"),
                    };
            return Task.FromResult(r);
        }
    }

    [Fact]
    public async Task GetGitHubPrivateFlagsAsync_ParsesPrivateField()
    {
        var creds = new StubCredentialService();
        var handler = new StubHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"private\": true}"),
            },
        };
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com/") };
        var sut = new GitHubRepositoryMetadataService(creds, http);
        var repo = new Repo { Name = "R", Url = "https://github.com/o/r.git", Branch = "main" };

        var results = await sut.GetGitHubPrivateFlagsAsync([repo]);

        Assert.Single(results);
        Assert.Same(repo, results[0].Repo);
        Assert.True(results[0].GitHubReportsPrivate);
    }

    [Fact]
    public async Task GetGitHubPrivateFlagsAsync_WhenNotGitHub_ReturnsNull()
    {
        var creds = new StubCredentialService();
        var handler = new StubHandler();
        var http = new HttpClient(handler);
        var sut = new GitHubRepositoryMetadataService(creds, http);
        var repo = new Repo { Name = "R", Url = "https://gitlab.com/a/b.git", Branch = "main" };

        var results = await sut.GetGitHubPrivateFlagsAsync([repo]);

        Assert.Single(results);
        Assert.Null(results[0].GitHubReportsPrivate);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetGitHubPrivateFlagsAsync_WhenAnonymousOk_DoesNotSendTokenEvenIfStored()
    {
        var creds = new StubCredentialService { Token = "ghp_testtoken" };
        var handler = new StubHandler();
        var http = new HttpClient(handler);
        var sut = new GitHubRepositoryMetadataService(creds, http);
        var repo = new Repo { Name = "MyRepo", Url = "https://github.com/o/r.git", Branch = "main" };

        await sut.GetGitHubPrivateFlagsAsync([repo]);

        var req = Assert.Single(handler.Requests);
        Assert.Null(req.Headers.Authorization);
    }

    [Fact]
    public async Task GetGitHubPrivateFlagsAsync_WhenAnonymousNotFound_RetriesWithClassicTokenScheme()
    {
        var creds = new StubCredentialService { Token = "ghp_testtoken" };
        var handler = new NotFoundThenOkHandler();
        var http = new HttpClient(handler);
        var sut = new GitHubRepositoryMetadataService(creds, http);
        var repo = new Repo { Name = "MyRepo", Url = "https://github.com/o/r.git", Branch = "main" };

        await sut.GetGitHubPrivateFlagsAsync([repo]);

        Assert.Equal(2, handler.RequestCount);
        Assert.Null(handler.AuthScheme[0]);
        Assert.Equal("token", handler.AuthScheme[1]);
    }

    [Fact]
    public async Task GetGitHubPrivateFlagsAsync_WhenAnonymousNotFound_RetriesWithFineGrainedBearerScheme()
    {
        var pat = "github_pat_" + new string('x', 20);
        var creds = new StubCredentialService { Token = pat };
        var handler = new NotFoundThenOkHandler();
        var http = new HttpClient(handler);
        var sut = new GitHubRepositoryMetadataService(creds, http);
        var repo = new Repo { Name = "MyRepo", Url = "https://github.com/o/r.git", Branch = "main" };

        await sut.GetGitHubPrivateFlagsAsync([repo]);

        Assert.Equal(2, handler.RequestCount);
        Assert.Null(handler.AuthScheme[0]);
        Assert.Equal("Bearer", handler.AuthScheme[1]);
    }

    private sealed class NotFoundThenOkHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        public List<string?> AuthScheme { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            AuthScheme.Add(request.Headers.Authorization?.Scheme);
            if (RequestCount == 1)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"private\": true}"),
            });
        }
    }
}
