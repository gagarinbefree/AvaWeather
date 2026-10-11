using Avalonia;
using Avalonia.Media;
using AvaWeather.Theming;
using AvaWeather.ViewModels;

namespace AvaWeather.Views;

public sealed class WeatherIcon : SvgIcon
{
    public static readonly StyledProperty<WeatherConditionKind> KindProperty =
        AvaloniaProperty.Register<WeatherIcon, WeatherConditionKind>(nameof(Kind));
    public static readonly StyledProperty<IBrush> AccentBrushProperty =
        AvaloniaProperty.Register<WeatherIcon, IBrush>(nameof(AccentBrush),
            ThemeColorService.Default.GetBrush("WeatherIcon.DefaultAccent"));

    public WeatherConditionKind Kind { get => GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public IBrush AccentBrush { get => GetValue(AccentBrushProperty); set => SetValue(AccentBrushProperty, value); }

    public WeatherIcon(double size = 32, IBrush? foreground = null)
        : base(WeatherCondition.IconFor(WeatherConditionKind.ClearDay), size,
            foreground ?? ThemeColorService.Default.GetBrush("WeatherIcon.DefaultAccent"))
    {
        if (foreground is not null)
            AccentBrush = foreground;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == KindProperty)
            AssetName = WeatherCondition.IconFor(Kind);
        if (change.Property == AccentBrushProperty)
            TintBrush = AccentBrush;
    }
}
