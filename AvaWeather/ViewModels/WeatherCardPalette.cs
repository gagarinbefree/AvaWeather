using Avalonia.Media;
using AvaWeather.Theming;

namespace AvaWeather.ViewModels;

public sealed record WeatherCardColors(IBrush CurrentBackground, IBrush ForecastBackground, IBrush Accent);

public static class WeatherCardPalette
{
    private static readonly IReadOnlyDictionary<WeatherConditionKind, WeatherCardColors> Palettes =
        Enum.GetValues<WeatherConditionKind>().ToDictionary(kind => kind, kind =>
        {
            var prefix = $"Weather.{kind}";
            var theme = ThemeColorService.Default;
            return new WeatherCardColors(theme.GetBrush($"{prefix}.Current"),
                theme.GetBrush($"{prefix}.Forecast"), theme.GetBrush($"{prefix}.Accent"));
        });

    public static WeatherCardColors For(WeatherConditionKind condition) => Palettes[condition];
}
