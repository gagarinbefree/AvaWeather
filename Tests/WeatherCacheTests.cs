using Application.Caching;
using Application.Extensions;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class WeatherCacheTests
{
    [Fact]
    public async Task Saves_and_restores_weather_and_metadata()
    {
        var storage = new MemoryStorage();
        var cache = new WeatherCache(storage);
        var data = new WeatherData { Name = "Perm", Country = "Russia", Lat = 58, Lon = 56,
            Current = new CurrentWeather { TempC = 12, ConditionText = "Облачно" },
            HourlyForecast = [new HourlyForecast { TempC = 11 }],
            DailyForecast = [new DailyForecast { MaxtempC = 14 }] };
        var timestamp = new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

        await cache.SaveAsync(data, timestamp, "ru", TestContext.Current.CancellationToken);
        var result = await cache.LoadAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(timestamp, result.FetchedAtUtc);
        Assert.Equal("ru", result.Language);
        Assert.Equal("Perm", result.Weather.Name);
        Assert.Equal("Облачно", result.Weather.Current!.ConditionText);
        Assert.Equal(11, result.Weather.HourlyForecast[0].TempC);
        Assert.Equal(14, result.Weather.DailyForecast[0].MaxtempC);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not json")]
    [InlineData("{\"version\":999}")]
    [InlineData("{\"version\":1,\"language\":\"ru\",\"fetchedAtUtc\":\"2026-10-19T09:00:00Z\",\"weather\":null}")]
    public async Task Missing_or_invalid_cache_is_ignored(string? json)
    {
        Assert.Null(await new WeatherCache(new MemoryStorage { Value = json }).LoadAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Invalid_weather_is_not_saved()
    {
        var storage = new MemoryStorage { Value = "previous" };
        await new WeatherCache(storage).SaveAsync(new WeatherData(), DateTimeOffset.UtcNow, "ru", TestContext.Current.CancellationToken);
        Assert.Equal("previous", storage.Value);
    }

    [Fact]
    public void Application_registers_cache_with_replaceable_storage()
    {
        var services = new ServiceCollection();
        var storage = new MemoryStorage();
        services.AddSingleton<IWeatherCacheStorage>(storage);
        services.AddApplication();
        using var provider = services.BuildServiceProvider();
        Assert.Same(storage, provider.GetRequiredService<IWeatherCacheStorage>());
        Assert.NotNull(provider.GetRequiredService<IWeatherCache>());
    }

    private sealed class MemoryStorage : IWeatherCacheStorage
    {
        public string? Value { get; set; }
        public Task<string?> ReadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Value);
        public Task WriteAsync(string value, CancellationToken cancellationToken = default)
        {
            Value = value;
            return Task.CompletedTask;
        }
    }
}
