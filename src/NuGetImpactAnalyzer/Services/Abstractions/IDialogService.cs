using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Abstraction for modal UI flows so view models stay free of WPF window types.
/// </summary>
public interface IDialogService : IRepositoryCredentialsDialogLauncher
{
    /// <summary>
    /// Shows a modal editor for <paramref name="repo"/> (name, URL, branch). Returns <c>true</c> if the user confirmed.
    /// </summary>
    /// <param name="onCredentialsDialogClosed">Invoked each time the nested credentials dialog closes.</param>
    /// <param name="isRepositoryUrlAlreadyUsed">When set, called with the trimmed URL; return <c>true</c> if another repo already uses this URL.</param>
    bool ShowEditRepositoryDialog(
        Repo repo,
        RepositoryEditorDialogKind kind = RepositoryEditorDialogKind.Edit,
        Action? onCredentialsDialogClosed = null,
        Func<string, bool>? isRepositoryUrlAlreadyUsed = null);
}
