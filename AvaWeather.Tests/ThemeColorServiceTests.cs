using Avalonia.Media;
using AvaWeather.Theming;
using AvaWeather.ViewModels;

namespace AvaWeather.Tests;

public class ThemeColorServiceTests
{
    [Fact]
    public void Default_theme_returns_named_colors()
    {
        Assert.Equal("#E7EEF8", ThemeColorService.Default.GetHex("Weather.Snow.Current"));
        Assert.Equal(Color.Parse("#E7EEF8"), ThemeColorService.Default.GetColor("Weather.Snow.Current"));
    }

    [Fact]
    public void Every_weather_condition_has_a_complete_palette()
    {
        foreach (var kind in Enum.GetValues<WeatherConditionKind>())
        {
            Assert.NotNull(ThemeColorService.Default.GetBrush($"Weather.{kind}.Current"));
            Assert.NotNull(ThemeColorService.Default.GetBrush($"Weather.{kind}.Forecast"));
            Assert.NotNull(ThemeColorService.Default.GetBrush($"Weather.{kind}.Accent"));
        }
    }

    [Fact]
    public void Unknown_color_name_fails_clearly()
    {
        Assert.Throws<KeyNotFoundException>(() => ThemeColorService.Default.GetHex("Missing"));
    }
}
