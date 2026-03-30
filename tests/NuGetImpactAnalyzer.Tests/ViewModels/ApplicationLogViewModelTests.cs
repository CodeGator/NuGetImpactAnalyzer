using CommunityToolkit.Mvvm.Input;
using NuGetImpactAnalyzer.ViewModels;

namespace NuGetImpactAnalyzer.Tests.ViewModels;

public sealed class ApplicationLogViewModelTests
{
    [Fact]
    public void AppendLine_AppendsMessageAndNewline()
    {
        var sut = new ApplicationLogViewModel();

        sut.AppendLine("hello");
        sut.AppendLine("world");

        Assert.Equal("hello" + Environment.NewLine + "world" + Environment.NewLine, sut.Text);
    }

    [Fact]
    public void Text_IsInitiallyEmpty()
    {
        var sut = new ApplicationLogViewModel();

        Assert.Equal(string.Empty, sut.Text);
    }

    [Fact]
    public void ClearLogCommand_ClearsText()
    {
        var sut = new ApplicationLogViewModel();
        sut.AppendLine("x");

        ((IRelayCommand)sut.ClearLogCommand).Execute(null);

        Assert.Equal(string.Empty, sut.Text);
    }
}
