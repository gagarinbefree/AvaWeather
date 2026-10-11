using Application.Theming;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace AvaWeather.Theming;

public static class AvaloniaThemeExtensions
{
    public static Color GetColor(this ThemeColorService theme, string name) =>
        Color.Parse(theme.GetHex(name));

    public static IBrush GetBrush(this ThemeColorService theme, string name) =>
        new ImmutableSolidColorBrush(theme.GetColor(name));
}
