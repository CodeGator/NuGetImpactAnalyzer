namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Narrow dialog surface for launching the per-repository HTTPS credential editor (keeps edit view models free of full <see cref="IDialogService"/> and avoids DI cycles).
/// </summary>
public interface IRepositoryCredentialsDialogLauncher
{
    /// <param name="onClosed">Invoked after the dialog closes (save, clear, or cancel).</param>
    void ShowRepositoryCredentialsDialog(IRepositoryCredentialContext context, Action? onClosed = null);
}
