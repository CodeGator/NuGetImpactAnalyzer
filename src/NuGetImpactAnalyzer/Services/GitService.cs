using System.IO;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

public sealed class GitService : IGitService
{
    private readonly string _dataRoot;
    private readonly ICredentialService _credentials;

    public GitService(ICredentialService credentials)
        : this(AppDataLocations.DefaultRepositoriesRoot(), credentials)
    {
    }

    public GitService(string dataRoot, ICredentialService credentials)
    {
        _dataRoot = dataRoot;
        _credentials = credentials;
    }

    /// <inheritdoc />
    public bool TryDeleteLocalClone(Repo repo)
    {
        var path = GetLocalRepoPath(repo);
        try
        {
            if (!Directory.Exists(path))
            {
                return true;
            }

            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(path, recursive: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public Task CloneOrUpdateAsync(Repo repo, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => CloneOrUpdateCore(repo, progress, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public async Task SyncAllAsync(IEnumerable<Repo> repos, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        foreach (var repo in repos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await CloneOrUpdateAsync(repo, progress, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                progress?.Report(FormatLine($"{repo.Name}: ERROR — {ex.Message}"));
            }
        }
    }

    private void CloneOrUpdateCore(Repo repo, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(repo.Url))
        {
            throw new ArgumentException("Repository URL is required.", nameof(repo));
        }

        var localPath = GetLocalRepoPath(repo);
        Directory.CreateDirectory(_dataRoot);

        if (!Directory.Exists(localPath))
        {
            CloneRepository(repo, localPath, progress);
            return;
        }

        if (!Repository.IsValid(localPath))
        {
            throw new InvalidOperationException(
                $"Path '{localPath}' exists but is not a valid Git repository. Remove the folder or change the repository name in config.");
        }

        progress?.Report(FormatLine($"{repo.Name}: updating existing clone at {localPath}"));
        try
        {
            using var git = new Repository(localPath);
            PullLatest(git, repo, progress);
        }
        catch (LibGit2SharpException ex)
        {
            throw new InvalidOperationException($"Git error while updating '{repo.Name}': {ex.Message}", ex);
        }

        progress?.Report(FormatLine($"{repo.Name}: update finished."));
    }

    private void CloneRepository(Repo repo, string localPath, IProgress<string>? progress)
    {
        progress?.Report(FormatLine($"{repo.Name}: cloning {repo.Url} (branch: {EffectiveBranch(repo)})"));

        var cloneOptions = new CloneOptions();
        var branch = EffectiveBranch(repo);
        if (!string.IsNullOrEmpty(branch))
        {
            cloneOptions.BranchName = branch;
        }

        ApplyHttpsCredentials(repo, cloneOptions.FetchOptions);

        try
        {
            Repository.Clone(repo.Url, localPath, cloneOptions);
        }
        catch (LibGit2SharpException ex)
        {
            throw new InvalidOperationException($"Git error while cloning '{repo.Name}': {ex.Message}", ex);
        }

        progress?.Report(FormatLine($"{repo.Name}: clone finished."));
    }

    private void PullLatest(Repository repository, Repo repo, IProgress<string>? progress)
    {
        var branchName = EffectiveBranch(repo);
        var signature = new Signature("NuGetImpactAnalyzer", "nuget-impact@local", DateTimeOffset.Now);

        progress?.Report(FormatLine($"{repo.Name}: fetching from origin"));
        var fetchOptions = new FetchOptions();
        ApplyHttpsCredentials(repo, fetchOptions);
        Commands.Fetch(repository, "origin", Array.Empty<string>(), fetchOptions, null);

        var remoteBranch = repository.Branches[$"origin/{branchName}"];
        if (remoteBranch is null)
        {
            throw new InvalidOperationException(
                $"Remote branch 'origin/{branchName}' was not found. Check the branch name and remote.");
        }

        var localBranch = repository.Branches[branchName];
        if (localBranch is null)
        {
            localBranch = repository.CreateBranch(branchName, remoteBranch.Tip);
            repository.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);
        }

        Commands.Checkout(repository, localBranch);

        progress?.Report(FormatLine($"{repo.Name}: pulling latest"));
        var pullOptions = new PullOptions
        {
            FetchOptions = new FetchOptions(),
        };
        ApplyHttpsCredentials(repo, pullOptions.FetchOptions);
        var mergeResult = Commands.Pull(repository, signature, pullOptions);

        if (mergeResult.Status == MergeStatus.Conflicts)
        {
            throw new InvalidOperationException(
                $"Merge conflicts while updating '{repo.Name}'. Resolve conflicts in the working tree, then try again.");
        }
    }

    /// <inheritdoc />
    public string GetRepositoriesRoot() => _dataRoot;

    /// <inheritdoc />
    public string? TryGetHeadCommitSha(string localRepositoryPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(localRepositoryPath) || !Repository.IsValid(localRepositoryPath))
            {
                return null;
            }

            using var repository = new Repository(localRepositoryPath);
            return repository.Head?.Tip?.Sha;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public string GetLocalRepositoryPath(Repo repo) => GetLocalRepoPath(repo);

    /// <inheritdoc />
    public bool IsLocalClonePresent(Repo repo)
    {
        try
        {
            var path = GetLocalRepoPath(repo);
            return Directory.Exists(path) && Repository.IsValid(path);
        }
        catch
        {
            return false;
        }
    }

    private string GetLocalRepoPath(Repo repo)
    {
        var folder = SanitizeFolderName(repo.Name);
        return Path.Combine(_dataRoot, folder);
    }

    private static string SanitizeFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "unnamed-repo";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Trim().ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (invalid.Contains(chars[i]))
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }

    private void ApplyHttpsCredentials(Repo repo, FetchOptions fetchOptions)
    {
        var token = _credentials.GetToken(repo.Name);
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        fetchOptions.CredentialsProvider = (_, _, _) =>
            new UsernamePasswordCredentials
            {
                Username = "git",
                Password = token,
            };
    }

    private static string EffectiveBranch(Repo repo) =>
        string.IsNullOrWhiteSpace(repo.Branch) ? "main" : repo.Branch.Trim();

    /// <inheritdoc />
    public bool TryProbeRemoteRepository(Repo repo)
    {
        if (string.IsNullOrWhiteSpace(repo.Url))
        {
            return false;
        }

        try
        {
            var refs = Repository.ListRemoteReferences(repo.Url, CreateRemoteCredentialsHandler(repo));
            foreach (var _ in refs)
            {
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ListBranches(Repo repo)
    {
        var localPath = GetLocalRepoPath(repo);
        if (Directory.Exists(localPath) && Repository.IsValid(localPath))
        {
            try
            {
                using var repository = new Repository(localPath);
                return ListBranchesFromLocalRepository(repository);
            }
            catch
            {
                // fall through to remote
            }
        }

        return ListRemoteBranchNames(repo);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ListProjectRelativePaths(Repo repo)
    {
        var root = GetLocalRepoPath(repo);
        if (!Directory.Exists(root))
        {
            return [];
        }

        return Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> ListBranchesFromLocalRepository(Repository repository)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var branch in repository.Branches)
        {
            if (!branch.IsRemote)
            {
                continue;
            }

            var fn = branch.FriendlyName;
            const string prefix = "origin/";
            if (!fn.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var shortName = fn[prefix.Length..];
            if (shortName.Contains("HEAD", StringComparison.Ordinal))
            {
                continue;
            }

            names.Add(shortName);
        }

        return names.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private IReadOnlyList<string> ListRemoteBranchNames(Repo repo)
    {
        if (string.IsNullOrWhiteSpace(repo.Url))
        {
            return [];
        }

        try
        {
            var refs = Repository.ListRemoteReferences(repo.Url, CreateRemoteCredentialsHandler(repo));
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var reference in refs)
            {
                var canonical = reference.CanonicalName;
                const string heads = "refs/heads/";
                if (!canonical.StartsWith(heads, StringComparison.Ordinal))
                {
                    continue;
                }

                names.Add(canonical[heads.Length..]);
            }

            return names.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch
        {
            return [];
        }
    }

    private CredentialsHandler CreateRemoteCredentialsHandler(Repo repo) =>
        (url, usernameFromUrl, types) =>
        {
            var token = _credentials.GetToken(repo.Name);
            if (string.IsNullOrEmpty(token))
            {
                return new DefaultCredentials();
            }

            return new UsernamePasswordCredentials
            {
                Username = "git",
                Password = token,
            };
        };

    private static string FormatLine(string message) =>
        $"[{DateTime.Now:HH:mm:ss}] {message}";
}
