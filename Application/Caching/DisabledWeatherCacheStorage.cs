namespace Application.Caching;

internal sealed class DisabledWeatherCacheStorage : IWeatherCacheStorage
{
    public Task<string?> ReadAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    public Task WriteAsync(string value, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
