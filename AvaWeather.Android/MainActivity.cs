using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace AvaWeather.Android;

[Activity(
    Label = "AvaWeather",
    Theme = "@style/AvaWeatherTheme",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public sealed class MainActivity : AvaloniaMainActivity
{
}
