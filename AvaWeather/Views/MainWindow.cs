using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using AvaWeather.Theming;

namespace AvaWeather.Views;

public sealed class MainWindow : Window
{
    public MainWindow()
    {
        Title = "AvaWeather";
        Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://AvaWeather/Assets/app-icon.png")));
        Width = 1180;
        Height = 810;
        MinWidth = 490;
        MinHeight = 520;
        Background = ThemeColorService.Default.GetBrush("Page");
    }
}
