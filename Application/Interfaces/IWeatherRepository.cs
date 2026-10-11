using Domain.Entities;

namespace Application.Interfaces;

public interface IWeatherRepository
{
    Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default);
}
