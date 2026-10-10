using System.Globalization;
using System.Text.Json;
using Application.Localization;
using Avalonia.Media;
using AvaWeather.Localization;
using Domain.Entities;
using Weather.Localization;
using FluentIconKind = FluentIcons.Common.Icon;

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
    public IBrush HeaderBackground => WeatherCardPalette.For(IconKind).HeaderBackground;

    public static WeatherDisplay From(WeatherData data, UiStrings strings)
    {
        var culture = strings.Culture;
        var current = data.Current ?? throw new InvalidOperationException(strings.Get("CurrentWeatherMissing"));
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
        StringLocalizer.For(culture).GetOrDefault($"WindDirection{value}", value);
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
    private sealed class Entry
    {
        public string Kind { get; set; } = string.Empty;
        public string? NightKind { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string WidgetSymbol { get; set; } = string.Empty;
        public int[] Codes { get; set; } = [];
    }

    private static readonly (Dictionary<int, (WeatherConditionKind Day, WeatherConditionKind Night)> Codes,
        Dictionary<WeatherConditionKind, FluentIconKind> Icons,
        Dictionary<WeatherConditionKind, string> Symbols) Catalog = Load();

    public static WeatherConditionKind For(int code, bool isDay) =>
        Catalog.Codes.TryGetValue(code, out var entry)
            ? isDay ? entry.Day : entry.Night
            : WeatherConditionKind.PartlyCloudy;

    public static FluentIconKind IconFor(WeatherConditionKind kind) =>
        Catalog.Icons.TryGetValue(kind, out var icon) ? icon : FluentIconKind.WeatherPartlyCloudyDay;

    public static string WidgetSymbolFor(WeatherConditionKind kind) => Catalog.Symbols[kind];

    private static (Dictionary<int, (WeatherConditionKind Day, WeatherConditionKind Night)>,
        Dictionary<WeatherConditionKind, FluentIconKind>, Dictionary<WeatherConditionKind, string>) Load()
    {
        using var stream = typeof(WeatherCondition).Assembly.GetManifestResourceStream("AvaWeather.Assets.weather-conditions.json")
            ?? throw new InvalidDataException(StringLocalizer.Current.Get("ConditionCatalogMissing"));
        var entries = JsonSerializer.Deserialize<Entry[]>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];
        var codes = new Dictionary<int, (WeatherConditionKind, WeatherConditionKind)>();
        var icons = new Dictionary<WeatherConditionKind, FluentIconKind>();
        var symbols = new Dictionary<WeatherConditionKind, string>();
        foreach (var entry in entries)
        {
            var day = Enum.Parse<WeatherConditionKind>(entry.Kind);
            var night = entry.NightKind is null ? day : Enum.Parse<WeatherConditionKind>(entry.NightKind);
            icons.Add(day, Enum.Parse<FluentIconKind>(entry.Icon));
            symbols.Add(day, entry.WidgetSymbol);
            foreach (var code in entry.Codes) codes.Add(code, (day, night));
        }
        return (codes, icons, symbols);
    }
}
