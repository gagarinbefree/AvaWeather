using AvaWeather.Services;
using Domain.Entities;
using AvaWeather.ViewModels;

namespace AvaWeather.Tests;

public class WeatherLocationMonitorTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;
    private static readonly IpLocation Perm = new("Perm", "Perm Krai", "Russia", 58.0, 56.25);
    private static readonly IpLocation Kazan = new("Kazan", "Tatarstan", "Russia", 55.79, 49.12);

    [Fact]
    public async Task Unchanged_location_does_not_request_weather_again()
    {
        var locations = new FakeLocations(Perm, Perm);
        var weather = new FakeWeather();
        var monitor = new WeatherLocationMonitor(locations, weather);

        await monitor.LoadAsync(Cancellation);
        var changed = await monitor.CheckAsync(Cancellation);

        Assert.Null(changed);
        Assert.Equal(2, locations.Calls);
        Assert.Equal([Perm], weather.RequestedLocations);
    }

    [Fact]
    public async Task Changed_location_fetches_new_forecast_and_remembers_it()
    {
        var locations = new FakeLocations(Perm, Kazan, Kazan);
        var weather = new FakeWeather();
        var monitor = new WeatherLocationMonitor(locations, weather);

        await monitor.LoadAsync(Cancellation);
        var changed = await monitor.CheckAsync(Cancellation);
        var unchanged = await monitor.CheckAsync(Cancellation);

        Assert.Equal("Kazan", changed?.Name);
        Assert.Null(unchanged);
        Assert.Equal([Perm, Kazan], weather.RequestedLocations);
    }

    [Fact]
    public async Task Failed_weather_refresh_retries_new_location_on_next_check()
    {
        var locations = new FakeLocations(Perm, Kazan, Kazan);
        var weather = new FakeWeather { FailNextLocation = Kazan };
        var monitor = new WeatherLocationMonitor(locations, weather);

        await monitor.LoadAsync(Cancellation);
        await Assert.ThrowsAsync<HttpRequestException>(() => monitor.CheckAsync(Cancellation));
        var changed = await monitor.CheckAsync(Cancellation);

        Assert.Equal("Kazan", changed?.Name);
        Assert.Equal([Perm, Kazan, Kazan], weather.RequestedLocations);
    }

    [Fact]
    public async Task Failed_geolocation_keeps_last_weather_and_retries()
    {
        var locations = new FakeLocations(Perm, null, Kazan);
        var weather = new FakeWeather();
        var monitor = new WeatherLocationMonitor(locations, weather);

        await monitor.LoadAsync(Cancellation);
        await Assert.ThrowsAsync<HttpRequestException>(() => monitor.CheckAsync(Cancellation));
        var changed = await monitor.CheckAsync(Cancellation);

        Assert.Equal("Kazan", changed?.Name);
        Assert.Equal([Perm, Kazan], weather.RequestedLocations);
    }

    [Fact]
    public async Task View_model_updates_city_without_clearing_existing_forecast()
    {
        var locations = new FakeLocations(Perm, Kazan);
        var weather = new FakeWeather();
        var monitor = new WeatherLocationMonitor(locations, weather);
        var viewModel = new WeatherViewModel(weather, monitor);

        await viewModel.LoadAsync();
        Assert.Equal("Perm", viewModel.Weather?.Name);
        await viewModel.CheckLocationAsync();

        Assert.Equal("Kazan", viewModel.Weather?.Name);
        Assert.NotNull(viewModel.Display);
        Assert.Null(viewModel.ErrorMessage);
        Assert.False(viewModel.IsLoading);
    }

    private sealed class FakeLocations(params IpLocation?[] responses) : IIpLocationClient
    {
        public int Calls { get; private set; }
        public Task<IpLocation> LocateAsync(CancellationToken cancellationToken = default)
        {
            var response = responses[Calls++];
            return response is null
                ? Task.FromException<IpLocation>(new HttpRequestException("offline"))
                : Task.FromResult(response);
        }
    }

    private sealed class FakeWeather : IWeatherRepository
    {
        public List<IpLocation> RequestedLocations { get; } = [];
        public IpLocation? FailNextLocation { get; set; }
        public Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new WeatherData { Name = "Fallback" });

        public Task<WeatherData> GetWeatherAsync(IpLocation location, CancellationToken cancellationToken = default)
        {
            RequestedLocations.Add(location);
            if (FailNextLocation == location)
            {
                FailNextLocation = null;
                return Task.FromException<WeatherData>(new HttpRequestException("offline"));
            }
            return Task.FromResult(new WeatherData { Name = location.City, Country = location.Country,
                Current = new CurrentWeather() });
        }
    }
}
