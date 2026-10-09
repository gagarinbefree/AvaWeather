using AvaWeather.Widgets;
using Domain.Entities;

namespace AvaWeather.Tests;

public class WidgetSnapshotTests
{
    [Fact]
    public void Sunny_weather_uses_soft_yellow_and_russian_labels()
    {
        var weather = Weather("Russia", 1000, 1);

        var snapshot = WidgetSnapshot.From(weather);

        Assert.Equal("Москва", snapshot.City);
        Assert.Equal("24°", snapshot.Temperature);
        Assert.Equal("Ощущается как 22°", snapshot.FeelsLike);
        Assert.Equal("#FFF0B8", snapshot.Background);
        Assert.Equal("#7C5700", snapshot.Foreground);
        Assert.Equal("sun.max.fill", snapshot.Symbol);
    }

    [Fact]
    public void Rain_and_night_have_distinct_palettes()
    {
        Assert.Equal("#D7EAF5", WidgetSnapshot.From(Weather("Germany", 1183, 1)).Background);
        var night = WidgetSnapshot.From(Weather("Germany", 1000, 0));
        Assert.Equal("#DCE5F5", night.Background);
        Assert.Equal("moon.stars.fill", night.Symbol);
        Assert.Equal("Feels like 22°", night.FeelsLike);
    }

    [Fact]
    public void Invalid_current_conditions_cannot_create_a_widget()
    {
        Assert.Throws<InvalidDataException>(() => WidgetSnapshot.From(new WeatherData()));
    }

    private static WeatherData Weather(string country, int code, int isDay) => new()
    {
        Name = "Москва",
        Country = country,
        Current = new CurrentWeather
        {
            TempC = 24.3,
            FeelslikeC = 21.7,
            ConditionCode = code,
            ConditionText = "Sunny",
            IsDay = isDay,
            Humidity = 47
        }
    };
}
