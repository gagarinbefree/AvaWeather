using Domain.Entities;

namespace Application.Caching;

public interface IWeatherCache
{
    Task<CachedWeather?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(WeatherData weather, DateTimeOffset fetchedAt, string language,
        CancellationToken cancellationToken = default);
}
