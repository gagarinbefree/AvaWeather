using System.Globalization;
using Application.Localization;
using AvaWeather.ViewModels;
using Domain.Entities;
using Weather.Localization;
using AvaWeather.Theming;

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
        var theme = ThemeColorService.Default;
        var background = theme.GetHex($"Weather.{kind}.Current");
        var foreground = theme.GetHex($"Weather.{kind}.Accent");
        var symbol = WeatherCondition.WidgetSymbolFor(kind);
        var degrees = new Func<double, string>(value => $"{Math.Round(value, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture)}°");
        return new WidgetSnapshot(
            weather.Name, degrees(current.TempC),
            $"{strings.Get("WidgetFeelsLike")} {degrees(current.FeelslikeC)}",
            current.ConditionText,
            $"{strings.Get("Humidity")} {current.Humidity}%",
            background, foreground, symbol);
    }
}
