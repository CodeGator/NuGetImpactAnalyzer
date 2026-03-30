namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Distinguishes add vs edit flows for the repository editor modal.
/// </summary>
public enum RepositoryEditorDialogKind
{
    Add,
    Edit,
}
