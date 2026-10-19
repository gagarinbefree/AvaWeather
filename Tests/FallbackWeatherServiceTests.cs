using Application.Interfaces;
using Application.Services;
using Domain.Entities;

namespace Tests;

public class FallbackWeatherServiceTests
{
    private static readonly IpLocation Location = new("Perm", "Perm", "Russia", 58, 56);

    [Fact]
    public async Task Uses_next_source_after_timeout()
    {
        var first = new Stub(_ => Task.FromException<WeatherData>(new TaskCanceledException()));
        var second = new Stub(location => Task.FromResult(new WeatherData { Name = location.City }));
        var result = await new FallbackWeatherService([first, second]).GetWeatherAsync(Location, TestContext.Current.CancellationToken);
        Assert.Equal("Perm", result.Name);
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task Stops_after_success()
    {
        var first = new Stub(_ => Task.FromResult(new WeatherData()));
        var second = new Stub(_ => throw new InvalidOperationException());
        await new FallbackWeatherService([first, second]).GetWeatherAsync(Location, TestContext.Current.CancellationToken);
        Assert.Equal(0, second.Calls);
    }

    [Fact]
    public async Task Does_not_ignore_caller_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var first = new Stub(_ => Task.FromException<WeatherData>(new OperationCanceledException()));
        var second = new Stub(_ => Task.FromResult(new WeatherData()));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new FallbackWeatherService([first, second]).GetWeatherAsync(Location, cancellation.Token));
        Assert.Equal(0, second.Calls);
    }

    private sealed class Stub(Func<IpLocation, Task<WeatherData>> action) : IFallbackWeatherService
    {
        public int Calls { get; private set; }
        public Task<WeatherData> GetWeatherAsync(IpLocation location, CancellationToken cancellationToken = default)
        {
            Calls++;
            return action(location);
        }
    }
}
