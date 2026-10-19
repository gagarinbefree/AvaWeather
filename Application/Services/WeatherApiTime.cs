using System.Globalization;

namespace Application.Services;

/// <summary>Parses WeatherAPI's local wall-clock timestamps in the Gregorian calendar.</summary>
public static class WeatherApiTime
{
    public static bool TryParseTimestamp(string? value, out DateTime result) =>
        DateTime.TryParseExact(value, "yyyy-MM-dd H:mm", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out result);

    public static bool TryParseDate(string? value, out DateTime result) =>
        DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out result);

    public static DateTime TimestampOrDefault(string? value) =>
        TryParseTimestamp(value, out var result) ? result : default;

    public static DateTime DateOrDefault(string? value) =>
        TryParseDate(value, out var result) ? result : default;
}
