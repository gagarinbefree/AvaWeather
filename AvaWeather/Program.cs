using Avalonia;
using Avalonia.Media;
using AvaWeather.Theming;

namespace AvaWeather;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new FontManagerOptions { DefaultFamilyName = WeatherTypography.FontFamily })
        .LogToTrace();
}
