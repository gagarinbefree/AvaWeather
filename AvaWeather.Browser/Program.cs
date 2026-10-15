using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices.JavaScript;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Media;
using AvaWeather.Theming;
using Infrastructure.Configuration;
using AvaWeather.ViewModels;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("browser")]

namespace AvaWeather.Browser;

internal static class Program
{
    public static WeatherApiOptions Options { get; private set; } = new();

    public static async Task Main(string[] args)
    {
        var page = new Uri(args[0], UriKind.Absolute);
        using var client = new HttpClient { BaseAddress = page };
        try
        {
            BrowserSettings? settings = null;
#if DEBUG
            try
            {
                settings = await client.GetFromJsonAsync("appsettings.Development.json",
                    BrowserJsonContext.Default.BrowserSettings);
            }
            catch (HttpRequestException)
            {
            }
#endif
            settings ??= await client.GetFromJsonAsync("appsettings.json",
                BrowserJsonContext.Default.BrowserSettings);
            if (settings?.WeatherApi is not null)
                Options = settings.WeatherApi;
        }
        catch (HttpRequestException)
        {
            // The dashboard still starts and shows the weather service error.
        }

        Options.BaseUrl = string.IsNullOrWhiteSpace(Options.BaseUrl)
            ? "https://api.weatherapi.com/v1/" : Options.BaseUrl;
        Options.DefaultLocation = "auto:ip";
        Options.ForecastDays = 3;
        Options.RequestTimeoutSeconds = 10;
        Options.UseDirectConnectionFallback = false;
        await JSHost.ImportAsync("forecastAccessibility", new Uri(page, "accessibility.js").AbsoluteUri);
        await AppBuilder.Configure<BrowserApp>()
            .With(new FontManagerOptions { DefaultFamilyName = WeatherTypography.FontFamily })
            .StartBrowserAppAsync("out");
    }

}

public sealed class BrowserSettings
{
    public WeatherApiOptions? WeatherApi { get; set; }
}

[JsonSerializable(typeof(BrowserSettings))]
[JsonSerializable(typeof(AccessibleForecast))]
internal partial class BrowserJsonContext : JsonSerializerContext;
