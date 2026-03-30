using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using NuGetImpactAnalyzer.Core;

namespace NuGetImpactAnalyzer.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        Title = $"About {AppConstants.ApplicationTitle}";
        TitleText.Text = AppConstants.ApplicationTitle;
        VersionText.Text = $"Version {ApplicationAbout.GetProductVersion()}";
        CopyrightText.Text = ApplicationAbout.FormatCopyrightLine();

        if (TryLoadAboutIcon() is { } bmp)
        {
            AboutIcon.Source = bmp;
        }
        else
        {
            AboutIcon.Visibility = Visibility.Collapsed;
        }
    }

    private static BitmapImage? TryLoadAboutIcon()
    {
        var packUri = new Uri("pack://application:,,,/Assets/icon.png", UriKind.Absolute);
        if (TryDecode(packUri) is { } fromPack)
        {
            return fromPack;
        }

        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.png");
        if (!File.Exists(path))
        {
            return null;
        }

        return TryDecode(new Uri(path, UriKind.Absolute));
    }

    private static BitmapImage? TryDecode(Uri uri)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = uri;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e) => Close();
}
