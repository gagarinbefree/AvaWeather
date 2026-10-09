using System.Globalization;
using Application.Localization;
using AvaWeather.ViewModels;
using Domain.Entities;

namespace AvaWeather.Widgets;

public sealed record WidgetSnapshot(
    string City, string Temperature, string FeelsLike, string Condition,
    string Humidity, string Background, string Foreground, string Symbol)
{
    public static WidgetSnapshot From(WeatherData weather)
    {
        var current = weather.Current ?? throw new InvalidDataException("Current weather is missing.");
        if (string.IsNullOrWhiteSpace(weather.Name))
            throw new InvalidDataException("Weather location is missing.");

        var russian = WeatherLanguage.ForCountry(weather.Country).TwoLetterISOLanguageName == "ru";
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
            $"{(russian ? "Ощущается как" : "Feels like")} {degrees(current.FeelslikeC)}",
            current.ConditionText,
            $"{(russian ? "Влажность" : "Humidity")} {current.Humidity}%",
            background, foreground, symbol);
    }
}
