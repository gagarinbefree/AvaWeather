using Application.Dtos;
using Domain.Entities;

namespace Application.Services;

public interface IWeatherDataService
{
    WeatherData MapToWeatherData(CurrentResponseDto current, ForecastResponseDto forecast);
}