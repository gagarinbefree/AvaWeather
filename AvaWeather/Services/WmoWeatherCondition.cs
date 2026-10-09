using System.Globalization;
using Weather.Localization;

namespace AvaWeather.Services;

public static class WmoWeatherCondition
{
    public static string Description(int code, bool russian)
    {
        var culture = CultureInfo.GetCultureInfo(russian ? "ru" : "en");
        return StringLocalizer.For(culture).WeatherCondition(code);
    }
}
