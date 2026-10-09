using Domain.Entities;

namespace AvaWeather.Services;

public interface IOpenMeteoWeatherService
{
    Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default);
}
