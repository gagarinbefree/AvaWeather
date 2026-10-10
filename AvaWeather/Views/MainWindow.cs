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
        Height = 820;
        MinWidth = 490;
        MinHeight = 520;
        CanResize = true;
        Background = ThemeColorService.Default.GetBrush("Page");
        WindowDecorations = WindowDecorations.None;
        ExtendClientAreaToDecorationsHint = true;
    }
}
