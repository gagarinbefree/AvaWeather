using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaWeather.ViewModels;
using FluentIcons.Avalonia;
using FluentIcons.Common;
using FluentIconKind = FluentIcons.Common.Icon;

namespace AvaWeather.Views;

public sealed class WeatherIcon : Grid
{
    private readonly FluentIcon _icon;

    public static readonly StyledProperty<WeatherConditionKind> KindProperty =
        AvaloniaProperty.Register<WeatherIcon, WeatherConditionKind>(nameof(Kind));

    public WeatherConditionKind Kind { get => GetValue(KindProperty); set => SetValue(KindProperty, value); }

    public WeatherIcon(double size = 32, IBrush? foreground = null)
    {
        Width = size;
        Height = size;
        _icon = new FluentIcon
        {
            Icon = FluentIconKind.WeatherPartlyCloudyDay,
            IconVariant = IconVariant.Regular,
            FontSize = size,
            Width = size,
            Height = size,
            Foreground = foreground ?? Brushes.SteelBlue,
            IsHitTestVisible = false
        };
        Children.Add(_icon);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == KindProperty && _icon is not null)
            _icon.Icon = IconKind(Kind);
    }

    private static FluentIconKind IconKind(WeatherConditionKind condition) => condition switch
    {
        WeatherConditionKind.ClearDay => FluentIconKind.WeatherSunny,
        WeatherConditionKind.ClearNight => FluentIconKind.WeatherMoon,
        WeatherConditionKind.PartlyCloudy => FluentIconKind.WeatherPartlyCloudyDay,
        WeatherConditionKind.PartlyCloudyNight => FluentIconKind.WeatherPartlyCloudyNight,
        WeatherConditionKind.Cloudy => FluentIconKind.WeatherCloudy,
        WeatherConditionKind.Fog => FluentIconKind.WeatherFog,
        WeatherConditionKind.Rain => FluentIconKind.WeatherRain,
        WeatherConditionKind.Snow => FluentIconKind.WeatherSnow,
        WeatherConditionKind.Thunder => FluentIconKind.WeatherThunderstorm,
        _ => FluentIconKind.WeatherPartlyCloudyDay
    };
}
