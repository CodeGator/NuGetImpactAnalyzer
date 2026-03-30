using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;
using NuGetImpactAnalyzer.ViewModels;

namespace NuGetImpactAnalyzer.Infrastructure;

/// <summary>
/// Supplies dialog view models with minimal dependencies so <see cref="DialogService"/> does not construct them directly.
/// </summary>
public sealed class DialogViewModelFactory : IDialogViewModelFactory
{
    private readonly IRepositoryCredentialsDialogLauncher _credentialsDialogs;
    private readonly ICredentialService _credentials;
    private readonly IGitService _gitService;
    private readonly IGitHubRepositoryMetadataService _gitHubMetadata;

    public DialogViewModelFactory(
        IRepositoryCredentialsDialogLauncher credentialsDialogs,
        ICredentialService credentials,
        IGitService gitService,
        IGitHubRepositoryMetadataService gitHubMetadata)
    {
        _credentialsDialogs = credentialsDialogs;
        _credentials = credentials;
        _gitService = gitService;
        _gitHubMetadata = gitHubMetadata;
    }

    /// <inheritdoc />
    public EditRepositoryViewModel CreateEditRepositoryViewModel(
        Repo repo,
        RepositoryEditorDialogKind kind,
        Action? onCredentialsDialogClosed = null,
        Func<string, bool>? isRepositoryUrlAlreadyUsed = null) =>
        new(repo, _credentialsDialogs, _gitService, _gitHubMetadata, kind, onCredentialsDialogClosed, isRepositoryUrlAlreadyUsed);

    /// <inheritdoc />
    public RepositoryCredentialsViewModel CreateRepositoryCredentialsViewModel(IRepositoryCredentialContext context) =>
        new(context, _credentials);
}
