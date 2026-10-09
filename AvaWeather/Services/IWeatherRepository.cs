using Domain.Entities;

namespace AvaWeather.Services;

public interface IWeatherRepository
{
    Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default);
}
