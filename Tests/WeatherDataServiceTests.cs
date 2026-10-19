using Application.Dtos;
using Application.Services;
using AutoMapper;
using Infrastructure.Mappers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Globalization;

namespace Tests;

public class WeatherDataServiceTests
{
    [Theory]
    [InlineData("2030-04-20 6:30")]
    [InlineData("2030-04-20 06:30")]
    public void Location_time_accepts_single_or_double_digit_hours(string localTime)
    {
        Assert.True(WeatherApiTime.TryParseTimestamp(localTime, out var parsed));
        Assert.Equal(new DateTime(2030, 4, 20, 6, 30, 0), parsed);
        Assert.Equal(DateTimeKind.Unspecified, parsed.Kind);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("ru-RU")]
    [InlineData("ar-SA")]
    public void Api_dates_use_invariant_calendar_and_skip_invalid_forecasts(string cultureName)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
            using var provider = services.BuildServiceProvider();
            var service = new WeatherDataService(provider.GetRequiredService<IMapper>());
            var current = new CurrentResponseDto
            {
                Location = new LocationDto { LocalTime = "2030-04-20 23:30" },
                Current = new CurrentDataDto { LastUpdated = "2030-04-20 23:15" }
            };
            var forecast = new ForecastResponseDto { Forecast = new ForecastDto { ForecastDay =
            [
                new ForecastDayDto { Date = "invalid", Hour = [new HourDto { Time = "invalid" }] },
                new ForecastDayDto { Date = "2030-04-21", Hour =
                [new HourDto { Time = "2030-04-21 01:00" }, new HourDto { Time = "2030-04-21 00:00" }] },
                new ForecastDayDto { Date = "2030-04-22", Hour = [new HourDto { Time = "2030-04-22 00:00" }] }
            ] } };

            var result = service.MapToWeatherData(current, forecast);
            Assert.Equal(new DateTime(2030, 4, 20, 23, 30, 0), result.LocalTime);
            Assert.Equal(new DateTime(2030, 4, 20, 23, 15, 0), result.Current!.LastUpdated);
            Assert.Equal([new DateTime(2030, 4, 21, 0, 0, 0), new DateTime(2030, 4, 21, 1, 0, 0)], result.HourlyForecast.Select(x => x.Time));
            Assert.Equal([new DateTime(2030, 4, 21), new DateTime(2030, 4, 22)], result.DailyForecast.Select(x => x.Date));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("2030-02-30 10:00")]
    public void Invalid_location_time_is_rejected_instead_of_using_device_clock(string localTime)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
        using var provider = services.BuildServiceProvider();
        var service = new WeatherDataService(provider.GetRequiredService<IMapper>());
        var current = new CurrentResponseDto { Location = new LocationDto { LocalTime = localTime } };

        Assert.Throws<InvalidDataException>(() => service.MapToWeatherData(current, new ForecastResponseDto()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    public void Missing_or_invalid_update_time_is_not_replaced_by_device_time(string lastUpdated)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
        using var provider = services.BuildServiceProvider();
        var service = new WeatherDataService(provider.GetRequiredService<IMapper>());
        var current = new CurrentResponseDto
        {
            Location = new LocationDto { LocalTime = "2030-04-20 15:30" },
            Current = new CurrentDataDto { LastUpdated = lastUpdated }
        };

        var result = service.MapToWeatherData(current, new ForecastResponseDto());
        Assert.Equal(default, result.Current!.LastUpdated);
        Assert.Equal(string.Empty, Application.Presentation.WeatherDashboard.From(result).Updated);
    }

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
