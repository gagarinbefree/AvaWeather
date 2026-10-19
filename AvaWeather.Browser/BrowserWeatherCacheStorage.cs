using System.Runtime.InteropServices.JavaScript;
using Application.Caching;

namespace AvaWeather.Browser;

public sealed partial class BrowserWeatherCacheStorage : IWeatherCacheStorage
{
    [JSImport("readWeatherCache", "weatherCache")]
    private static partial string? Read();

    [JSImport("writeWeatherCache", "weatherCache")]
    private static partial void Write(string value);

    public Task<string?> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Read());
    }

    public Task WriteAsync(string value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Write(value);
        return Task.CompletedTask;
    }
}
