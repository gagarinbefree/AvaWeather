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

    [Fact]
    public async Task Detected_country_selects_language_and_number_format()
    {
        var date = new DateTime(2030, 4, 20, 15, 30, 0);
        var viewModel = new WeatherViewModel(new FakeRepository(() => Task.FromResult(new WeatherData
        {
            Name = "Kyiv", Country = "Ukraine", LocalTime = date,
            Current = new CurrentWeather { TempC = 12.5, WindKph = 10.5, WindDir = "NE" },
            DailyForecast = [new DailyForecast { Date = date.Date, Sunrise = "06:30 AM", Sunset = "07:00 PM" }]
        })));

        await viewModel.LoadAsync();

        Assert.Equal("ru-RU", viewModel.Strings.Culture.Name);
        Assert.Equal("Kyiv, Украина", viewModel.Display!.Location);
        Assert.Equal("12,5°C", viewModel.Display.Temperature);
        Assert.Equal("10,5 км/ч СВ", viewModel.Display.Wind);
        Assert.Equal("06:30 / 19:00", Assert.Single(viewModel.Display.Daily).SunriseSunset);
    }

    [Fact]
    public async Task Non_russian_country_keeps_english_interface()
    {
        var viewModel = new WeatherViewModel(new FakeRepository(() => Task.FromResult(new WeatherData
        {
            Name = "Berlin", Country = "Germany", Current = new CurrentWeather { TempC = 12.5 }
        })));

        await viewModel.LoadAsync();

        Assert.Equal("en-US", viewModel.Strings.Culture.Name);
        Assert.Equal("Hourly Forecast", viewModel.Strings.HourlyForecast);
        Assert.Equal("Berlin, Germany", viewModel.Display!.Location);
        Assert.Equal("12.5°C", viewModel.Display.Temperature);
    }

    private sealed class FakeRepository(Func<Task<WeatherData>> load) : IWeatherRepository
    {
        public Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default) => load();
    }
}
