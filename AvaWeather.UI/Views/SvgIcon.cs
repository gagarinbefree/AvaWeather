using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Svg.Skia;
using Svg.Model;

namespace AvaWeather.Views;

public class SvgIcon : Image
{
    private static readonly Uri AssetRoot = new("avares://AvaWeather.UI/");
    private static readonly ConcurrentDictionary<(string Name, Color Color), SvgImage> Images = new();

    public static readonly StyledProperty<string> AssetNameProperty =
        AvaloniaProperty.Register<SvgIcon, string>(nameof(AssetName), "WeatherSunny");

    public static readonly StyledProperty<IBrush> TintBrushProperty =
        AvaloniaProperty.Register<SvgIcon, IBrush>(nameof(TintBrush), Brushes.Black);

    public string AssetName { get => GetValue(AssetNameProperty); set => SetValue(AssetNameProperty, value); }
    public IBrush TintBrush { get => GetValue(TintBrushProperty); set => SetValue(TintBrushProperty, value); }

    public SvgIcon(string name = "WeatherSunny", double size = 20, IBrush? tint = null)
    {
        Width = size;
        Height = size;
        Stretch = Stretch.Uniform;
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        IsHitTestVisible = false;
        AssetName = name;
        if (tint is not null)
            TintBrush = tint;
        RefreshSource();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AssetNameProperty || change.Property == TintBrushProperty)
            RefreshSource();
    }

    private void RefreshSource()
    {
        var color = TintBrush is ISolidColorBrush solid ? solid.Color : Colors.Black;
        Source = Images.GetOrAdd((AssetName, color), static key =>
        {
            var source = SvgSource.Load($"avares://AvaWeather.UI/Assets/Icons/{key.Name}.svg", AssetRoot);
            source.ReLoad(new SvgParameters(null,
                $"path {{ fill: #{key.Color.R:X2}{key.Color.G:X2}{key.Color.B:X2}; }}"));
            return new SvgImage { Source = source };
        });
    }
}
