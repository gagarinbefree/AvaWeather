using AvaWeather.Services;
using Domain.Entities;

namespace AvaWeather.Tests;

public class WeatherSourceSelectionTests
{
    [Fact]
    public async Task Successful_open_meteo_response_does_not_call_weather_api()
    {
        var expected = new WeatherData { Name = "Perm", Current = new CurrentWeather() };
        var repository = new WeatherRepository(null!, null!, null!, new StubPrimary(_ => Task.FromResult(expected)), null!);

        var result = await repository.GetWeatherAsync(TestContext.Current.CancellationToken);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_converted_into_fallback()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var repository = new WeatherRepository(null!, null!, null!,
            new StubPrimary(token => Task.FromCanceled<WeatherData>(token)), null!);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetWeatherAsync(cancelled.Token));
    }

    private sealed class StubPrimary(Func<CancellationToken, Task<WeatherData>> load) : IOpenMeteoWeatherService
    {
        public Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default) => load(cancellationToken);
    }
}
