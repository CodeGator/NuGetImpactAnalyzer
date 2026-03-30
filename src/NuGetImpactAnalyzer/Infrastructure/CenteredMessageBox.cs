using System.Windows;
using System.Windows.Controls;

namespace NuGetImpactAnalyzer.Infrastructure;

/// <summary>
/// WPF confirmation dialogs that honor <see cref="WindowStartupLocation.CenterOwner"/>.
/// System <see cref="MessageBox.Show(Window, string, string, MessageBoxButton, MessageBoxImage)"/> often centers on the screen instead.
/// </summary>
public static class CenteredMessageBox
{
    public static MessageBoxResult Show(
        Window? owner,
        string messageBoxText,
        string caption,
        MessageBoxButton button,
        MessageBoxImage icon)
    {
        if (owner is null || button != MessageBoxButton.YesNo)
        {
            return MessageBox.Show(owner, messageBoxText, caption, button, icon);
        }

        return ShowYesNoCentered(owner, messageBoxText, caption, icon);
    }

    private static MessageBoxResult ShowYesNoCentered(
        Window owner,
        string text,
        string caption,
        MessageBoxImage icon)
    {
        MessageBoxResult result = MessageBoxResult.No;

        var window = new Window
        {
            Title = caption,
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SizeToContent = SizeToContent.WidthAndHeight,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            MinWidth = 320,
        };

        var root = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };

        if (icon != MessageBoxImage.None)
        {
            var body = new DockPanel { LastChildFill = true };
            var iconBlock = new TextBlock
            {
                Text = GetIconGlyph(icon),
                FontSize = 28,
                Margin = new Thickness(0, 0, 14, 0),
                VerticalAlignment = VerticalAlignment.Top,
            };
            DockPanel.SetDock(iconBlock, Dock.Left);
            body.Children.Add(iconBlock);
            body.Children.Add(new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 420,
            });
            root.Children.Add(body);
        }
        else
        {
            root.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 420 });
        }

        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0),
        };

        var yes = new Button { Content = "Yes", IsDefault = true, MinWidth = 80, Margin = new Thickness(0, 0, 8, 0) };
        yes.Click += (_, _) =>
        {
            result = MessageBoxResult.Yes;
            window.DialogResult = true;
        };

        var no = new Button { Content = "No", IsCancel = true, MinWidth = 80 };
        no.Click += (_, _) =>
        {
            result = MessageBoxResult.No;
            window.DialogResult = false;
        };

        buttonRow.Children.Add(yes);
        buttonRow.Children.Add(no);
        root.Children.Add(buttonRow);
        window.Content = root;

        window.ShowDialog();

        return result;
    }

    private static string GetIconGlyph(MessageBoxImage icon) =>
        icon switch
        {
            MessageBoxImage.Warning => "\u26A0",
            MessageBoxImage.Error => "\u2715",
            MessageBoxImage.Information => "\u2139",
            MessageBoxImage.Question => "?",
            _ => string.Empty,
        };
}
