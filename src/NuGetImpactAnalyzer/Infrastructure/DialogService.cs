using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;
using NuGetImpactAnalyzer.ViewModels;
using NuGetImpactAnalyzer.Views;

namespace NuGetImpactAnalyzer.Infrastructure;

/// <summary>
/// WPF implementation: resolves dialog view models via <see cref="IDialogViewModelFactory"/>, applies owner window, runs modal loop.
/// </summary>
public sealed class DialogService : IDialogService
{
    private readonly IServiceProvider _services;
    private readonly Stack<Window> _modalOwnerStack = new();

    public DialogService(IServiceProvider services)
    {
        _services = services;
    }

    /// <inheritdoc />
    public bool ShowEditRepositoryDialog(
        Repo repo,
        RepositoryEditorDialogKind kind = RepositoryEditorDialogKind.Edit,
        Action? onCredentialsDialogClosed = null,
        Func<string, bool>? isRepositoryUrlAlreadyUsed = null)
    {
        var factory = _services.GetRequiredService<IDialogViewModelFactory>();
        var viewModel = factory.CreateEditRepositoryViewModel(repo, kind, onCredentialsDialogClosed, isRepositoryUrlAlreadyUsed);
        var window = new EditRepositoryWindow(viewModel)
        {
            Owner = DialogOwnerWindow.Resolve(),
        };
        _modalOwnerStack.Push(window);
        try
        {
            return window.ShowDialog() == true;
        }
        finally
        {
            _modalOwnerStack.Pop();
        }
    }

    /// <inheritdoc />
    public void ShowRepositoryCredentialsDialog(IRepositoryCredentialContext context, Action? onClosed = null)
    {
        var factory = _services.GetRequiredService<IDialogViewModelFactory>();
        var vm = factory.CreateRepositoryCredentialsViewModel(context);
        var window = new RepositoryCredentialsWindow(vm)
        {
            Owner = _modalOwnerStack.TryPeek(out var owner) ? owner : DialogOwnerWindow.Resolve(),
        };
        try
        {
            window.ShowDialog();
        }
        finally
        {
            onClosed?.Invoke();
        }
    }
}
