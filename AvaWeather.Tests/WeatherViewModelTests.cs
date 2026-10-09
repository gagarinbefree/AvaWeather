using AvaWeather.Services;
using AvaWeather.ViewModels;
using Domain.Entities;

namespace AvaWeather.Tests;

public class WeatherViewModelTests
{
    [Fact]
    public async Task Load_populates_dashboard_and_clears_loading_state()
    {
        var expected = new WeatherData { Name = "Moscow", Current = new CurrentWeather { TempC = 12 } };
        var viewModel = new WeatherViewModel(new FakeRepository(() => Task.FromResult(expected)));

        await viewModel.LoadAsync();

        Assert.Same(expected, viewModel.Weather);
        Assert.False(viewModel.IsLoading);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Failed_load_shows_connection_error_and_retry_recovers()
    {
        var calls = 0;
        var expected = new WeatherData { Name = "Moscow", Current = new CurrentWeather() };
        var viewModel = new WeatherViewModel(new FakeRepository(() =>
        {
            calls++;
            return calls == 1
                ? Task.FromException<WeatherData>(new HttpRequestException("offline"))
                : Task.FromResult(expected);
        }));

        await viewModel.LoadAsync();
        Assert.Contains("internet connection", viewModel.ErrorMessage);
        Assert.Null(viewModel.Weather);

        await viewModel.LoadAsync();
        Assert.Same(expected, viewModel.Weather);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Concurrent_loads_use_one_request()
    {
        var response = new TaskCompletionSource<WeatherData>();
        var calls = 0;
        var viewModel = new WeatherViewModel(new FakeRepository(() =>
        {
            calls++;
            return response.Task;
        }));

        var first = viewModel.LoadAsync();
        var second = viewModel.LoadAsync();
        Assert.True(viewModel.IsLoading);
        Assert.Equal(1, calls);

        response.SetResult(new WeatherData { Current = new CurrentWeather() });
        await Task.WhenAll(first, second);
        Assert.False(viewModel.IsLoading);
    }

    private sealed class FakeRepository(Func<Task<WeatherData>> load) : IWeatherRepository
    {
        public Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default) => load();
    }
}
