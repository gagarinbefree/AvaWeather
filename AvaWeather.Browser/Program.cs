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
    public static string? TelemetryDsn { get; private set; }

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
            TelemetryDsn = settings?.Telemetry?.Dsn;
        }
        catch (HttpRequestException)
        {
            // The dashboard still starts and shows the weather service error.
        }

        Options.BaseUrl = string.IsNullOrWhiteSpace(Options.BaseUrl)
            ? WeatherBackendConfiguration.BaseUrl : Options.BaseUrl;
        Options.DefaultLocation = "auto:ip";
        Options.ForecastDays = 3;
        Options.RequestTimeoutSeconds = 40;
        Options.UseDirectConnectionFallback = false;
        await JSHost.ImportAsync("forecastAccessibility", new Uri(page, "accessibility.js").AbsoluteUri);
        await JSHost.ImportAsync("deviceLocation", new Uri(page, "device-location.js").AbsoluteUri);
        await JSHost.ImportAsync("weatherCache", new Uri(page, "weather-cache.js").AbsoluteUri);
        await AppBuilder.Configure<BrowserApp>()
            .With(new FontManagerOptions { DefaultFamilyName = WeatherTypography.FontFamily })
            .StartBrowserAppAsync("out");
    }

}

public sealed class BrowserSettings
{
    public WeatherApiOptions? WeatherApi { get; set; }
    public BrowserTelemetrySettings? Telemetry { get; set; }
}

public sealed class BrowserTelemetrySettings
{
    public string? Dsn { get; set; }
}

[JsonSerializable(typeof(BrowserSettings))]
[JsonSerializable(typeof(AccessibleForecast))]
internal partial class BrowserJsonContext : JsonSerializerContext;
