using Domain.Entities;

namespace Application.Interfaces;

public interface IFallbackWeatherService
{
    Task<WeatherData> GetWeatherAsync(IpLocation location, CancellationToken cancellationToken = default);
}
