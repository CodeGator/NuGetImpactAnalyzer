using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// HTTPS token editor for a single repository name (Windows Credential Manager).
/// </summary>
public sealed partial class RepositoryCredentialsViewModel : ObservableObject, IDisposable
{
    private readonly IRepositoryCredentialContext _context;
    private readonly ICredentialService _credentials;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ClearStoredTokenCommand))]
    private bool _hasStoredToken;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTokenCommand))]
    private string _pendingToken = "";

    public RepositoryCredentialsViewModel(IRepositoryCredentialContext context, ICredentialService credentials)
    {
        _context = context;
        _credentials = credentials;
        _context.CredentialKeyNameChanged += OnCredentialKeyChanged;
        RefreshTokenState();
        NotifyCredentialKeyUi();
    }

    public void Dispose()
    {
        _context.CredentialKeyNameChanged -= OnCredentialKeyChanged;
    }

    private void OnCredentialKeyChanged(object? sender, EventArgs e)
    {
        PendingToken = string.Empty;
        RefreshTokenState();
        NotifyCredentialKeyUi();
        SaveTokenCommand.NotifyCanExecuteChanged();
        ClearStoredTokenCommand.NotifyCanExecuteChanged();
    }

    /// <summary>True when the parent edit dialog has a repository name (required to store credentials).</summary>
    public bool IsCredentialKeyMissing => string.IsNullOrWhiteSpace(_context.CredentialKeyName);

    private void NotifyCredentialKeyUi() => OnPropertyChanged(nameof(IsCredentialKeyMissing));

    private void RefreshTokenState()
    {
        var key = _context.CredentialKeyName;
        if (string.IsNullOrWhiteSpace(key))
        {
            HasStoredToken = false;
            return;
        }

        HasStoredToken = !string.IsNullOrEmpty(_credentials.GetToken(key.Trim()));
    }

    [RelayCommand(CanExecute = nameof(CanSaveToken))]
    private void SaveToken()
    {
        var key = _context.CredentialKeyName.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _credentials.SaveToken(key, PendingToken);
        HasStoredToken = true;
        PendingToken = string.Empty;
        ClearStoredTokenCommand.NotifyCanExecuteChanged();
    }

    private bool CanSaveToken()
    {
        var key = _context.CredentialKeyName.Trim();
        return !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(PendingToken);
    }

    [RelayCommand(CanExecute = nameof(CanClearStoredToken))]
    private void ClearStoredToken()
    {
        var key = _context.CredentialKeyName.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _credentials.DeleteToken(key);
        HasStoredToken = false;
        ClearStoredTokenCommand.NotifyCanExecuteChanged();
    }

    private bool CanClearStoredToken() => HasStoredToken;
}
