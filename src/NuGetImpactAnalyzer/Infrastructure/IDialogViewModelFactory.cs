using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;
using NuGetImpactAnalyzer.ViewModels;

namespace NuGetImpactAnalyzer.Infrastructure;

/// <summary>
/// Creates dialog-scoped view models without coupling views to concrete window services.
/// </summary>
public interface IDialogViewModelFactory
{
    EditRepositoryViewModel CreateEditRepositoryViewModel(
        Repo repo,
        RepositoryEditorDialogKind kind,
        Action? onCredentialsDialogClosed = null,
        Func<string, bool>? isRepositoryUrlAlreadyUsed = null);

    RepositoryCredentialsViewModel CreateRepositoryCredentialsViewModel(IRepositoryCredentialContext context);
}
