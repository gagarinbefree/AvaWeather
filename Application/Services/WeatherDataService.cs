using Application.Dtos;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Localization;

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
        if (!WeatherApiTime.TryParseTimestamp(current.Location.LocalTime, out var now))
            throw new InvalidDataException(StringLocalizer.Current.Get("WidgetInvalidResponse"));
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        var weatherData = _mapper.Map<WeatherData>(current.Location);
        weatherData.Current = _mapper.Map<CurrentWeather>(current.Current);
        if (!string.IsNullOrWhiteSpace(forecast.Current.Condition.Text))
            weatherData.Current.ConditionText = forecast.Current.Condition.Text;

        var filteredHours = forecast.Forecast.ForecastDay
            .SelectMany(d => d.Hour)
            .Select(h => (Hour: h, Valid: WeatherApiTime.TryParseTimestamp(h.Time, out var time), Time: time))
            .Where(h => h.Valid && ((h.Time.Date == today && h.Time >= now) || h.Time.Date == tomorrow))
            .OrderBy(h => h.Time)
            .Select(h => h.Hour)
            .ToList();

        weatherData.HourlyForecast = _mapper.Map<List<HourlyForecast>>(filteredHours);

        var dailyForecast = forecast.Forecast.ForecastDay
            .Where(d => WeatherApiTime.TryParseDate(d.Date, out _))
            .Take(3)
            .ToList();

        weatherData.DailyForecast = _mapper.Map<List<DailyForecast>>(dailyForecast);

        return weatherData;
    }
}
