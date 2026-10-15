using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using AvaWeather.ViewModels;

namespace AvaWeather.Browser;

internal static partial class BrowserAccessibility
{
    [JSImport("renderForecast", "forecastAccessibility")]
    private static partial void RenderForecast(string json);

    public static void Publish(WeatherViewModel viewModel)
    {
        var snapshot = AccessibleForecast.From(viewModel);
        RenderForecast(JsonSerializer.Serialize(snapshot, BrowserJsonContext.Default.AccessibleForecast));
    }
}
