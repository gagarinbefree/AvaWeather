using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Entities;

namespace Application.Caching;

public sealed class WeatherCache(IWeatherCacheStorage storage) : IWeatherCache
{
    private const int CurrentVersion = 1;

    public async Task<CachedWeather?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var json = await storage.ReadAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            var cached = JsonSerializer.Deserialize(json, WeatherCacheJsonContext.Default.CachedWeather);
            return IsValid(cached) ? cached : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task SaveAsync(WeatherData weather, DateTimeOffset fetchedAt, string language,
        CancellationToken cancellationToken = default)
    {
        var cached = new CachedWeather(CurrentVersion, fetchedAt.ToUniversalTime(), language, weather);
        if (!IsValid(cached)) return;
        var json = JsonSerializer.Serialize(cached, WeatherCacheJsonContext.Default.CachedWeather);
        await storage.WriteAsync(json, cancellationToken);
    }

    private static bool IsValid(CachedWeather? cached) =>
        cached is { Version: CurrentVersion, Weather.Current: not null, Weather.HourlyForecast: not null,
            Weather.DailyForecast: not null } &&
        cached.FetchedAtUtc != default && cached.FetchedAtUtc.Offset == TimeSpan.Zero &&
        !string.IsNullOrWhiteSpace(cached.Language) && !string.IsNullOrWhiteSpace(cached.Weather.Name);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CachedWeather))]
internal partial class WeatherCacheJsonContext : JsonSerializerContext
{
}
