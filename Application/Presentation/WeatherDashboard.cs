using System.Globalization;
using Application.Localization;
using Application.Weather;
using Domain.Entities;
using Weather.Localization;

namespace Application.Presentation;

public sealed record HourlyWeatherCard(string Time, string Temperature, string Condition,
    WeatherConditionDescriptor Weather, string Rain, bool IsNow);

public sealed record DailyWeatherCard(string Name, string Date, string Maximum, string Minimum,
    string Condition, WeatherConditionDescriptor Weather, string Humidity, string Wind,
    string Rain, string Uv, string SunriseSunset, bool IsToday);

public sealed record WeatherDashboard(
    CultureInfo Culture, StringLocalizer Strings, string Name, string Location, string Region,
    string Updated, string Temperature, string Condition, WeatherConditionDescriptor Weather,
    string FeelsLike, string Humidity, string Wind, string Uv, string Pressure, string Visibility,
    IReadOnlyList<HourlyWeatherCard> Hourly, IReadOnlyList<DailyWeatherCard> Daily)
{
    public static WeatherDashboard From(WeatherData data)
    {
        var culture = WeatherLanguage.ForCountry(data.Country);
        var strings = StringLocalizer.For(culture);
        var current = data.Current ?? throw new InvalidOperationException(strings.Get("CurrentWeatherMissing"));
        var today = data.LocalTime == default ? DateTime.Today : data.LocalTime.Date;

        return new WeatherDashboard(
            culture, strings, data.Name,
            $"{data.Name}, {WeatherLanguage.DisplayCountry(data.Country, culture)}",
            data.Region, current.LastUpdated.ToString("HH:mm", culture),
            Degrees(current.TempC, culture), Capitalize(current.ConditionText, culture),
            WeatherConditionCatalog.For(current.ConditionCode, current.IsDay == 1),
            Degrees(current.FeelslikeC, culture), $"{current.Humidity}%",
            $"{Number(current.WindKph, culture)} {strings.Get("KilometersPerHour")} {WindDirection(current.WindDir, culture)}".Trim(),
            Number(current.Uv, culture),
            $"{Number(current.PressureMb, culture)} {strings.Get("Millibars")}",
            $"{Number(current.VisKm, culture)} {strings.Get("Kilometers")}",
            data.HourlyForecast.Select(hour => new HourlyWeatherCard(
                hour.Time.ToString("HH:mm", culture), Degrees(hour.TempC, culture),
                Capitalize(hour.ConditionText, culture),
                WeatherConditionCatalog.For(hour.ConditionCode, hour.IsDay == 1),
                hour.ChanceOfRain > 0 ? $"{hour.ChanceOfRain}%" : string.Empty,
                hour.Time.Date == today && hour.Time.Hour == data.LocalTime.Hour)).ToArray(),
            data.DailyForecast.Select(day => new DailyWeatherCard(
                Capitalize(day.Date.Date == today ? strings.Get("Today") :
                    day.Date.Date == today.AddDays(1) ? strings.Get("Tomorrow") :
                    day.Date.ToString("dddd", culture), culture),
                day.Date.ToString("dd MMM yyyy", culture),
                Degrees(day.MaxtempC, culture), Degrees(day.MintempC, culture),
                Capitalize(day.ConditionText, culture),
                WeatherConditionCatalog.For(day.ConditionCode, true),
                $"{day.Avghumidity}%", $"{Number(day.MaxwindKph, culture)} {strings.Get("KilometersPerHour")}",
                $"{day.DailyChanceOfRain}%", Number(day.Uv, culture),
                $"{SunTime(day.Sunrise, culture)} / {SunTime(day.Sunset, culture)}",
                day.Date.Date == today)).ToArray());
    }

    private static string Number(double value, CultureInfo culture) => value.ToString("0.#", culture);
    private static string Degrees(double value, CultureInfo culture) => $"{Number(value, culture)}°C";

    private static string Capitalize(string value, CultureInfo culture)
    {
        var text = value.Trim();
        return text.Length == 0 ? text : char.ToUpper(text[0], culture) + text[1..];
    }

    private static string SunTime(string value, CultureInfo culture) =>
        culture.TwoLetterISOLanguageName == "ru" &&
        DateTime.TryParseExact(value, "hh:mm tt", CultureInfo.GetCultureInfo("en-US"),
            DateTimeStyles.None, out var time)
            ? time.ToString("HH:mm", culture) : value;

    private static string WindDirection(string value, CultureInfo culture) =>
        StringLocalizer.For(culture).GetOrDefault($"WindDirection{value}", value);
}
