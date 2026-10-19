namespace Application.Caching;

public interface IWeatherCacheStorage
{
    Task<string?> ReadAsync(CancellationToken cancellationToken = default);
    Task WriteAsync(string value, CancellationToken cancellationToken = default);
}
