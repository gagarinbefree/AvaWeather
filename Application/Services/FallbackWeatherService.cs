using System.Text.Json;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

public sealed class FallbackWeatherService(IReadOnlyList<IFallbackWeatherService> sources) : IFallbackWeatherService
{
    public async Task<WeatherData> GetWeatherAsync(IpLocation location, CancellationToken cancellationToken = default)
    {
        if (sources.Count == 0) throw new ArgumentException(nameof(sources));
        for (var i = 0; ; i++)
        {
            try { return await sources[i].GetWeatherAsync(location, cancellationToken); }
            catch (Exception error) when (i + 1 < sources.Count && !cancellationToken.IsCancellationRequested &&
                error is HttpRequestException or OperationCanceledException or JsonException or InvalidDataException) { }
        }
    }
}
