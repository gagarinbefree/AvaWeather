namespace AvaWeather.ViewModels;

public sealed record AccessibleForecast(
    string Language,
    string LinkLabel,
    string BackLabel,
    string Heading,
    string Status,
    bool IsLoading,
    string RetryLabel,
    string CurrentHeading,
    string Temperature,
    string Condition,
    string UpdatedLabel,
    string Updated,
    IReadOnlyList<AccessibleMetric> CurrentMetrics,
    string HourlyHeading,
    IReadOnlyList<AccessibleHour> Hourly,
    string DailyHeading,
    string HighLabel,
    string LowLabel,
    string HumidityLabel,
    string WindLabel,
    string RainLabel,
    string UvLabel,
    string SunriseSunsetLabel,
    IReadOnlyList<AccessibleDay> Daily)
{
    public static AccessibleForecast From(WeatherViewModel viewModel)
    {
        var strings = viewModel.Strings;
        var display = viewModel.HasWeather ? viewModel.Display : null;
        return new AccessibleForecast(
            strings.Culture.TwoLetterISOLanguageName,
            strings.Get("AccessibleForecastLink"),
            strings.Get("AccessibleBackToVisual"),
            display?.Location ?? viewModel.HeaderLocation,
            viewModel.IsLoading || display is null && viewModel.ErrorMessage is null
                ? strings.LoadingWeather : viewModel.ErrorMessage ?? string.Empty,
            viewModel.IsLoading || display is null && viewModel.ErrorMessage is null,
            strings.Retry,
            strings.Get("AccessibleCurrentWeather"),
            display?.Temperature ?? string.Empty,
            display?.Condition ?? string.Empty,
            strings.Get("AccessibleUpdated"),
            display?.Updated ?? string.Empty,
            display is null ? [] :
            [
                new(strings.FeelsLike, display.FeelsLike),
                new(strings.Humidity, display.Humidity),
                new(strings.Wind, display.Wind),
                new(strings.UvIndex, display.Uv),
                new(strings.Pressure, display.Pressure),
                new(strings.Visibility, display.Visibility)
            ],
            strings.HourlyForecast,
            display?.Hourly.Select(hour => new AccessibleHour(hour.Time, hour.IsNow ? strings.Now : string.Empty,
                hour.Temperature, hour.Condition, hour.Rain)).ToArray() ?? [],
            strings.ThreeDayForecast,
            strings.Get("AccessibleHigh"),
            strings.Get("AccessibleLow"),
            strings.Humidity,
            strings.Wind,
            strings.Rain,
            strings.Uv,
            strings.SunriseSunset,
            display?.Daily.Select(day => new AccessibleDay(day.Name, day.Date, day.Maximum, day.Minimum,
                day.Condition, day.Humidity, day.Wind, day.Rain, day.Uv, day.SunriseSunset)).ToArray() ?? []);
    }
}

public sealed record AccessibleMetric(string Label, string Value);
public sealed record AccessibleHour(string Time, string Now, string Temperature, string Condition, string Rain);
public sealed record AccessibleDay(string Name, string Date, string Maximum, string Minimum, string Condition,
    string Humidity, string Wind, string Rain, string Uv, string SunriseSunset);
