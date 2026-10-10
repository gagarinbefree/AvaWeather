using Avalonia;
using Avalonia.Media;
using AvaWeather.Theming;

namespace AvaWeather.ViewModels;

public sealed record WeatherCardColors(IBrush CurrentBackground, IBrush ForecastBackground, IBrush Accent,
    IBrush HeaderBackground);

public static class WeatherCardPalette
{
    private static readonly IReadOnlyDictionary<WeatherConditionKind, WeatherCardColors> Palettes =
        Enum.GetValues<WeatherConditionKind>().ToDictionary(kind => kind, kind =>
        {
            var prefix = $"Weather.{kind}";
            var theme = ThemeColorService.Default;
            return new WeatherCardColors(theme.GetBrush($"{prefix}.Current"),
                theme.GetBrush($"{prefix}.Forecast"), theme.GetBrush($"{prefix}.Accent"),
                Gradient(theme.GetColor($"{prefix}.Current"), theme.GetColor($"{prefix}.Forecast")));
        });

    public static IBrush NeutralHeader { get; } = Gradient(
        ThemeColorService.Default.GetColor("Header.Neutral.Start"),
        ThemeColorService.Default.GetColor("Header.Neutral.End"));

    public static WeatherCardColors For(WeatherConditionKind condition) => Palettes[condition];

    private static IBrush Gradient(Color start, Color end)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops = { new GradientStop(start, 0), new GradientStop(end, 1) }
        };
        return brush.ToImmutable();
    }
}
