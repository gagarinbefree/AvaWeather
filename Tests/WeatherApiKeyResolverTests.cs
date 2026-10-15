using AvaWeather.Services;

namespace Tests;

public class WeatherApiKeyResolverTests
{
    [Fact]
    public void Key_resolution_is_shared_with_the_weather_infrastructure()
    {
        Assert.Same(typeof(Infrastructure.Clients.WeatherApiClient).Assembly,
            typeof(WeatherApiKeyResolver).Assembly);
        Assert.Same(typeof(Infrastructure.Clients.WeatherApiClient).Assembly,
            typeof(EmbeddedWeatherApiKey).Assembly);
    }

    [Fact]
    public void Environment_key_takes_priority()
    {
        Assert.Equal("environment", WeatherApiKeyResolver.Resolve("environment", "settings", () => "embedded"));
    }

    [Fact]
    public void Local_settings_take_priority_over_embedded_key()
    {
        Assert.Equal("settings", WeatherApiKeyResolver.Resolve(null, "settings", () => "embedded"));
    }

    [Fact]
    public void Release_uses_embedded_key_when_no_override_exists()
    {
        Assert.Equal("embedded", WeatherApiKeyResolver.Resolve(null, "  ", () => "embedded"));
    }

    [Fact]
    public void Developer_build_without_key_returns_empty_value()
    {
        Assert.Equal(string.Empty, WeatherApiKeyResolver.Resolve(null, null, () => null));
    }
}
