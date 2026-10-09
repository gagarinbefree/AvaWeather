using System.Globalization;
using System.Resources;

namespace AvaWeather.Services;

public static class WmoWeatherCondition
{
    public static int IconCode(int code) => code switch
    {
        0 => 1000,
        1 or 2 => 1003,
        3 => 1009,
        45 or 48 => 1135,
        51 or 53 or 55 or 56 or 57 => 1153,
        61 or 63 or 65 or 66 or 67 => 1183,
        71 or 73 or 75 or 77 => 1213,
        80 or 81 or 82 => 1240,
        85 or 86 => 1255,
        95 or 96 or 97 or 99 => 1087,
        _ => 1003
    };

    private static readonly ResourceManager Resources =
        new("AvaWeather.Localization.WeatherConditions", typeof(WmoWeatherCondition).Assembly);

    public static string Description(int code, bool russian)
    {
        var culture = CultureInfo.GetCultureInfo(russian ? "ru" : "en");
        return Resources.GetString($"Code{code}", culture)
            ?? Resources.GetString("Unknown", culture)
            ?? throw new MissingManifestResourceException("Weather descriptions are missing.");
    }
}
