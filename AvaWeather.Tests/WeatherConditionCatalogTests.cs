using Application.Weather;

namespace AvaWeather.Tests;

public class WeatherConditionCatalogTests
{
    [Theory]
    [InlineData(1000, true, "ClearDay", "WeatherSunny")]
    [InlineData(1000, false, "ClearNight", "WeatherMoon")]
    [InlineData(1183, true, "Rain", "WeatherRain")]
    [InlineData(999999, true, "PartlyCloudy", "WeatherPartlyCloudyDay")]
    public void Shared_catalog_selects_weather_palette_and_icon(
        int code, bool isDay, string kind, string icon)
    {
        var condition = WeatherConditionCatalog.For(code, isDay);

        Assert.Equal(kind, condition.Kind);
        Assert.Equal(icon, condition.Icon);
    }
}
