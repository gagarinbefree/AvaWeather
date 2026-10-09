using AvaWeather.Services;

namespace AvaWeather.Tests;

public class WeatherSourceSelectionTests
{
    [Fact]
    public async Task Caller_cancellation_is_not_ignored_during_location_detection()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var repository = new WeatherRepository(null!, null!, null!,
            new CancelingLocation());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetWeatherAsync(cancelled.Token));
    }

    private sealed class CancelingLocation : IIpLocationClient
    {
        public Task<IpLocation> LocateAsync(CancellationToken cancellationToken = default) =>
            Task.FromCanceled<IpLocation>(cancellationToken);
    }
}
