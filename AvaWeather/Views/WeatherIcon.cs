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
    public static readonly StyledProperty<IBrush> AccentBrushProperty =
        AvaloniaProperty.Register<WeatherIcon, IBrush>(nameof(AccentBrush), Brushes.SteelBlue);

    public WeatherConditionKind Kind { get => GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public IBrush AccentBrush { get => GetValue(AccentBrushProperty); set => SetValue(AccentBrushProperty, value); }

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
        if (change.Property == AccentBrushProperty && _icon is not null)
            _icon.Foreground = AccentBrush;
    }

    private static FluentIconKind IconKind(WeatherConditionKind condition) => WeatherCondition.IconFor(condition);
}
