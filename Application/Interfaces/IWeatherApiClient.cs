using Application.Dtos;

namespace Application.Interfaces;

public interface IWeatherApiClient
{
    Task<CurrentResponseDto> GetCurrentWeatherAsync(string? location = null);
    Task<ForecastResponseDto> GetForecastAsync(string language = "en", string? location = null);
}
