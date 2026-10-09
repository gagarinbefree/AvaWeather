using Application.Dtos;

namespace Application.Interfaces;

public interface IWeatherApiClient
{
    Task<CurrentResponseDto> GetCurrentWeatherAsync();
    Task<ForecastResponseDto> GetForecastAsync();
}