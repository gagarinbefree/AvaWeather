using Domain.Entities;

namespace AvaWeather.Services;

public interface IWeatherRepository
{
    Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default);
    Task<WeatherData> GetWeatherAsync(IpLocation location, CancellationToken cancellationToken = default) =>
        GetWeatherAsync(cancellationToken);
}
