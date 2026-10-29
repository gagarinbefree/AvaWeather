using System.Text.Json;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services;

public sealed class FallbackWeatherService(IReadOnlyList<IFallbackWeatherService> sources) : IFallbackWeatherService
{
    public async Task<WeatherData> GetWeatherAsync(IpLocation location, CancellationToken cancellationToken = default)
    {
        if (sources.Count == 0) throw new ArgumentException(nameof(sources));
        WeatherData? retained = null;
        for (var i = 0; ; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await sources[i].GetWeatherAsync(location, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (result.ServerCachedAtUtc is null)
                {
                    return result;
                }
                if (retained is null || result.ServerCachedAtUtc > retained.ServerCachedAtUtc) retained = result;
            }
            catch (Exception error) when (!cancellationToken.IsCancellationRequested &&
                error is HttpRequestException or OperationCanceledException or JsonException or InvalidDataException)
            {
                if (error is WeatherBackendRateLimitException || i + 1 == sources.Count)
                {
                    if (retained is not null)
                    {
                        return retained;
                    }
                    throw;
                }
            }
            if (i + 1 == sources.Count)
            {
                return retained!;
            }
        }
    }
}
