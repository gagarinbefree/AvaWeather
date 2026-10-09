using System.Globalization;
using Application.Localization;
using AvaWeather.ViewModels;
using Domain.Entities;
using Weather.Localization;

namespace AvaWeather.Widgets;

public sealed record WidgetSnapshot(
    string City, string Temperature, string FeelsLike, string Condition,
    string Humidity, string Background, string Foreground, string Symbol)
{
    public static WidgetSnapshot From(WeatherData weather)
    {
        var current = weather.Current ?? throw new InvalidDataException(StringLocalizer.For(WeatherLanguage.ForCountry(weather.Country)).Get("CurrentWeatherMissing"));
        if (string.IsNullOrWhiteSpace(weather.Name))
            throw new InvalidDataException(StringLocalizer.For(WeatherLanguage.ForCountry(weather.Country)).Get("WeatherLocationMissing"));

        var strings = StringLocalizer.For(WeatherLanguage.ForCountry(weather.Country));
        var kind = WeatherCondition.For(current.ConditionCode, current.IsDay == 1);
        var (background, foreground, symbol) = kind switch
        {
            WeatherConditionKind.ClearDay => ("#FFF0B8", "#7C5700", "sun.max.fill"),
            WeatherConditionKind.ClearNight => ("#DCE5F5", "#344E78", "moon.stars.fill"),
            WeatherConditionKind.PartlyCloudy => ("#F4EBD4", "#6D5A2F", "cloud.sun.fill"),
            WeatherConditionKind.PartlyCloudyNight => ("#DCE5F5", "#344E78", "cloud.moon.fill"),
            WeatherConditionKind.Cloudy => ("#DFE8EF", "#435B70", "cloud.fill"),
            WeatherConditionKind.Rain => ("#D7EAF5", "#255B78", "cloud.rain.fill"),
            WeatherConditionKind.Snow => ("#E7EEF8", "#496382", "cloud.snow.fill"),
            WeatherConditionKind.Fog => ("#E7EAE5", "#52645B", "cloud.fog.fill"),
            WeatherConditionKind.Thunder => ("#E4DFF2", "#5A4F78", "cloud.bolt.rain.fill"),
            _ => ("#F4EBD4", "#6D5A2F", "cloud.sun.fill")
        };
        var degrees = new Func<double, string>(value => $"{Math.Round(value, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture)}°");
        return new WidgetSnapshot(
            weather.Name, degrees(current.TempC),
            $"{strings.Get("WidgetFeelsLike")} {degrees(current.FeelslikeC)}",
            current.ConditionText,
            $"{strings.Get("Humidity")} {current.Humidity}%",
            background, foreground, symbol);
    }
}
