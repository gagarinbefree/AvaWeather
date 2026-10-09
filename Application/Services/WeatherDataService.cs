using Application.Dtos;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using System.Globalization;

namespace Application.Services;

public class WeatherDataService : IWeatherDataService
{
    private readonly IMapper _mapper;

    public WeatherDataService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public WeatherData MapToWeatherData(CurrentResponseDto current, ForecastResponseDto forecast)
    {
        var now = DateTime.TryParseExact(current.Location.LocalTime, "yyyy-MM-dd HH:mm",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var locationTime)
            ? locationTime : DateTime.Now;
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        var weatherData = _mapper.Map<WeatherData>(current.Location);
        weatherData.Current = _mapper.Map<CurrentWeather>(current.Current);
        if (!string.IsNullOrWhiteSpace(forecast.Current.Condition.Text))
            weatherData.Current.ConditionText = forecast.Current.Condition.Text;

        var filteredHours = forecast.Forecast.ForecastDay
            .SelectMany(d => d.Hour)
            .Where(h =>
            {
                if (string.IsNullOrEmpty(h.Time))
                    return false;

                var time = DateTime.Parse(h.Time);
                return (time.Date == today && time >= now) || time.Date == tomorrow;
            })
            .OrderBy(h => DateTime.Parse(h.Time))
            .ToList();

        weatherData.HourlyForecast = _mapper.Map<List<HourlyForecast>>(filteredHours);

        var dailyForecast = forecast.Forecast.ForecastDay
            .Take(3)
            .ToList();

        weatherData.DailyForecast = _mapper.Map<List<DailyForecast>>(dailyForecast);

        return weatherData;
    }
}
