using Application.Presentation;
using Domain.Entities;

namespace AvaWeather.Tests;

public class WeatherDashboardTests
{
    [Fact]
    public void Russian_dashboard_formats_local_weather_and_day_names()
    {
        var data = new WeatherData
        {
            Name = "Пермь", Country = "Russia", Region = "Пермский край",
            LocalTime = new DateTime(2026, 10, 9, 15, 29, 0),
            Current = new CurrentWeather
            {
                LastUpdated = new DateTime(2026, 10, 9, 15, 15, 0),
                TempC = 8.9, ConditionCode = 1009, ConditionText = "пасмурно", IsDay = 1,
                WindKph = 9, Humidity = 63
            },
            HourlyForecast =
            [
                new HourlyForecast
                {
                    Time = new DateTime(2026, 10, 9, 15, 0, 0), TempC = 9,
                    ConditionCode = 1009, ConditionText = "пасмурно", IsDay = 1
                }
            ],
            DailyForecast =
            [
                new DailyForecast { Date = new DateTime(2026, 10, 9), ConditionCode = 1009 },
                new DailyForecast { Date = new DateTime(2026, 10, 10), ConditionCode = 1009 },
                new DailyForecast { Date = new DateTime(2026, 10, 11), ConditionCode = 1009 }
            ]
        };

        var dashboard = WeatherDashboard.From(data);

        Assert.Equal("ru", dashboard.Culture.TwoLetterISOLanguageName);
        Assert.Equal("Пермь, Россия", dashboard.Location);
        Assert.Equal("8,9°C", dashboard.Temperature);
        Assert.Equal("Пасмурно", dashboard.Condition);
        Assert.Equal("Cloudy", dashboard.Weather.Kind);
        Assert.True(Assert.Single(dashboard.Hourly).IsNow);
        Assert.Equal("Сегодня", dashboard.Daily[0].Name);
        Assert.Equal("Завтра", dashboard.Daily[1].Name);
        Assert.Equal("Воскресенье", dashboard.Daily[2].Name);
    }
}
