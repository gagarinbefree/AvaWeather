using Application.Dtos;
using Application.Services;
using AutoMapper;
using Infrastructure.Mappers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AvaWeather.Tests;

public class WeatherDataServiceTests
{
    [Fact]
    public void Hourly_forecast_uses_weather_locations_clock()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
        using var provider = services.BuildServiceProvider();
        var service = new WeatherDataService(provider.GetRequiredService<IMapper>());
        var current = new CurrentResponseDto
        {
            Location = new LocationDto { Name = "Moscow", LocalTime = "2030-04-20 15:30" },
            Current = new CurrentDataDto()
        };
        var forecast = new ForecastResponseDto
        {
            Forecast = new ForecastDto
            {
                ForecastDay =
                [
                    new ForecastDayDto
                    {
                        Date = "2030-04-20",
                        Hour =
                        [
                            new HourDto { Time = "2030-04-20 15:00" },
                            new HourDto { Time = "2030-04-20 16:00" }
                        ]
                    },
                    new ForecastDayDto
                    {
                        Date = "2030-04-21",
                        Hour = [new HourDto { Time = "2030-04-21 00:00" }]
                    }
                ]
            }
        };

        var result = service.MapToWeatherData(current, forecast);

        Assert.Equal(["2030-04-20 16:00", "2030-04-21 00:00"],
            result.HourlyForecast.Select(x => x.Time.ToString("yyyy-MM-dd HH:mm")));
    }

    [Fact]
    public void Forecast_localized_current_condition_replaces_initial_english_text()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
        using var provider = services.BuildServiceProvider();
        var service = new WeatherDataService(provider.GetRequiredService<IMapper>());
        var current = new CurrentResponseDto
        {
            Location = new LocationDto { Country = "Russia", LocalTime = "2030-04-20 15:30" },
            Current = new CurrentDataDto { Condition = new ConditionDto { Text = "Sunny" } }
        };
        var forecast = new ForecastResponseDto
        {
            Current = new CurrentDataDto { Condition = new ConditionDto { Text = "Солнечно" } }
        };

        var result = service.MapToWeatherData(current, forecast);

        Assert.Equal("Солнечно", result.Current!.ConditionText);
    }
}
