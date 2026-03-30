using CommunityToolkit.Mvvm.ComponentModel;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// Status line and busy/error state for the window footer.
/// </summary>
public partial class StatusBarViewModel : ObservableObject, IApplicationStatus
{
    [ObservableProperty]
    private string _message = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isError;

    public void SetReady(string message = "Ready")
    {
        IsBusy = false;
        IsError = false;
        Message = message;
    }

    public void SetBusy(string message)
    {
        IsBusy = true;
        IsError = false;
        Message = message;
    }

    public void SetError(string message)
    {
        IsBusy = false;
        IsError = true;
        Message = message;
    }
}
