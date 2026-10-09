using System.Globalization;
using Application.Localization;
using Avalonia.Media;
using AvaWeather.Localization;
using Domain.Entities;

namespace AvaWeather.ViewModels;

public sealed record WeatherDisplay(
    string Location,
    string Region,
    string Updated,
    string Temperature,
    string Condition,
    WeatherConditionKind IconKind,
    string FeelsLike,
    string Humidity,
    string Wind,
    string Uv,
    string Pressure,
    string Visibility,
    IReadOnlyList<HourDisplay> Hourly,
    IReadOnlyList<DayDisplay> Daily)
{
    public IBrush CardBackground => WeatherCardPalette.For(IconKind).CurrentBackground;
    public IBrush Accent => WeatherCardPalette.For(IconKind).Accent;

    public static WeatherDisplay From(WeatherData data, UiStrings strings)
    {
        var culture = strings.Culture;
        var current = data.Current ?? throw new InvalidOperationException("Current weather is missing.");
        var today = data.LocalTime == default ? DateTime.Today : data.LocalTime.Date;
        return new WeatherDisplay(
            $"{data.Name}, {WeatherLanguage.DisplayCountry(data.Country, culture)}", data.Region,
            current.LastUpdated.ToString("HH:mm", culture), Degrees(current.TempC, culture),
            Capitalize(current.ConditionText, culture), WeatherCondition.For(current.ConditionCode, current.IsDay == 1),
            Degrees(current.FeelslikeC, culture), $"{current.Humidity}%",
            $"{Number(current.WindKph, culture)} {strings.KilometersPerHour} {WindDirection(current.WindDir, culture)}".Trim(),
            Number(current.Uv, culture),
            $"{Number(current.PressureMb, culture)} {strings.Millibars}",
            $"{Number(current.VisKm, culture)} {strings.Kilometers}",
            data.HourlyForecast.Select(hour => new HourDisplay(
                hour.Time.ToString("HH:mm", culture), Degrees(hour.TempC, culture), Capitalize(hour.ConditionText, culture),
                WeatherCondition.For(hour.ConditionCode, hour.IsDay == 1),
                hour.ChanceOfRain > 0 ? $"{hour.ChanceOfRain}%" : string.Empty,
                hour.Time.Date == today && hour.Time.Hour == data.LocalTime.Hour, strings)).ToArray(),
            data.DailyForecast.Select(day => new DayDisplay(
                Capitalize(day.Date.Date == today ? strings.Today : day.Date.Date == today.AddDays(1) ? strings.Tomorrow : day.Date.ToString("dddd", culture), culture),
                day.Date.ToString("dd MMM yyyy", culture),
                Degrees(day.MaxtempC, culture), Degrees(day.MintempC, culture), Capitalize(day.ConditionText, culture),
                WeatherCondition.For(day.ConditionCode, true),
                $"{day.Avghumidity}%", $"{Number(day.MaxwindKph, culture)} {strings.KilometersPerHour}",
                $"{day.DailyChanceOfRain}%", Number(day.Uv, culture),
                $"{SunTime(day.Sunrise, culture)} / {SunTime(day.Sunset, culture)}", day.Date.Date == today, strings)).ToArray());
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
        DateTime.TryParseExact(value, "hh:mm tt", CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out var time)
            ? time.ToString("HH:mm", culture) : value;

    private static string WindDirection(string value, CultureInfo culture) =>
        culture.TwoLetterISOLanguageName == "ru" ? value switch
        {
            "N" => "С", "NNE" => "ССВ", "NE" => "СВ", "ENE" => "ВСВ",
            "E" => "В", "ESE" => "ВЮВ", "SE" => "ЮВ", "SSE" => "ЮЮВ",
            "S" => "Ю", "SSW" => "ЮЮЗ", "SW" => "ЮЗ", "WSW" => "ЗЮЗ",
            "W" => "З", "WNW" => "ЗСЗ", "NW" => "СЗ", "NNW" => "ССЗ",
            _ => value
        } : value;
}

public sealed record HourDisplay(string Time, string Temperature, string Condition, WeatherConditionKind IconKind, string Rain, bool IsNow, UiStrings Strings)
{
    public bool HasRain => Rain.Length > 0;
    public IBrush CardBackground => WeatherCardPalette.For(IconKind).ForecastBackground;
    public IBrush Accent => WeatherCardPalette.For(IconKind).Accent;
}

public sealed record DayDisplay(string Name, string Date, string Maximum, string Minimum, string Condition,
    WeatherConditionKind IconKind, string Humidity, string Wind, string Rain, string Uv, string SunriseSunset, bool IsToday, UiStrings Strings)
{
    public IBrush CardBackground => WeatherCardPalette.For(IconKind).ForecastBackground;
    public IBrush Accent => WeatherCardPalette.For(IconKind).Accent;
}

public enum WeatherConditionKind { ClearDay, ClearNight, PartlyCloudy, PartlyCloudyNight, Cloudy, Fog, Rain, Snow, Thunder }

public static class WeatherCondition
{
    public static WeatherConditionKind For(int code, bool isDay) => code switch
    {
        1000 => isDay ? WeatherConditionKind.ClearDay : WeatherConditionKind.ClearNight,
        1003 => isDay ? WeatherConditionKind.PartlyCloudy : WeatherConditionKind.PartlyCloudyNight,
        1006 or 1009 => WeatherConditionKind.Cloudy,
        1030 or 1135 or 1147 => WeatherConditionKind.Fog,
        1063 or 1150 or 1153 or 1168 or 1171 or 1180 or 1183 or 1186 or 1189 or 1192 or 1195 or 1198 or 1201 or 1240 or 1243 or 1246 => WeatherConditionKind.Rain,
        1066 or 1069 or 1072 or 1114 or 1117 or 1204 or 1207 or 1210 or 1213 or 1216 or 1219 or 1222 or 1225 or 1237 or 1249 or 1252 or 1255 or 1258 or 1261 or 1264 => WeatherConditionKind.Snow,
        1087 or 1273 or 1276 or 1279 or 1282 => WeatherConditionKind.Thunder,
        _ => WeatherConditionKind.PartlyCloudy
    };
}
