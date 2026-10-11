using Application.Extensions;
using Application.Theming;
using Application.Weather;
using Microsoft.Extensions.DependencyInjection;

namespace AvaWeather.Tests;

public class SharedThemeTests
{
    [Fact]
    public void Default_palette_is_owned_by_the_shared_application_assembly()
    {
        Assert.Equal(typeof(WeatherConditionCatalog).Assembly, typeof(ThemeColorService).Assembly);
        Assert.Equal("default", ThemeColorService.Default.Name);
        Assert.Equal("#F5F7FA", ThemeColorService.Default.GetHex("Page"));
        Assert.Equal("#FFF0B8", ThemeColorService.Default.GetHex("Weather.ClearDay.Current"));
    }

    [Fact]
    public void Every_weather_condition_has_complete_colors_and_header_gradient()
    {
        foreach (var kind in new[]
                 {
                     "ClearDay", "ClearNight", "PartlyCloudy", "PartlyCloudyNight",
                     "Cloudy", "Fog", "Rain", "Snow", "Thunder"
                 })
        {
            foreach (var role in new[] { "Current", "Forecast", "Accent", "HeaderStart", "HeaderEnd" })
                Assert.StartsWith("#", ThemeColorService.Default.GetHex($"Weather.{kind}.{role}"));
        }
    }

    [Fact]
    public void Application_di_exposes_the_same_theme_to_desktop_and_site()
    {
        using var services = new ServiceCollection().AddApplication().BuildServiceProvider();

        Assert.Same(ThemeColorService.Default, services.GetRequiredService<ThemeColorService>());
    }
}
