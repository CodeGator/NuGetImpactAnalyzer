using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// Observable text buffer for the output panel; implements <see cref="IApplicationLog"/> for services and coordinators.
/// </summary>
public partial class ApplicationLogViewModel : ObservableObject, IApplicationLog
{
    private readonly object _textLock = new();

    [ObservableProperty]
    private string _text = string.Empty;

    public void AppendLine(string message)
    {
        lock (_textLock)
        {
            Text += message + Environment.NewLine;
        }
    }

    /// <summary>Clears the output buffer (used by the Clear command and shell code).</summary>
    public void Clear()
    {
        lock (_textLock)
        {
            Text = string.Empty;
        }
    }

    [RelayCommand]
    private void ClearLog() => Clear();
}
