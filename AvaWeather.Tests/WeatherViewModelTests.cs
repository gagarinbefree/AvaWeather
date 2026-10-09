using AvaWeather.Services;
using AvaWeather.ViewModels;
using Domain.Entities;
using System.Net;

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

    [Fact]
    public async Task Weather_api_rejection_shows_status_instead_of_internet_error()
    {
        var viewModel = new WeatherViewModel(new FakeRepository(() =>
            Task.FromException<WeatherData>(new HttpRequestException(
                "WeatherAPI forecast request failed: Unauthorized", null, HttpStatusCode.Unauthorized))));

        await viewModel.LoadAsync();

        Assert.Contains("Unauthorized", viewModel.ErrorMessage);
        Assert.DoesNotContain("internet connection", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Russian_day_names_and_conditions_start_with_capital_letters_on_every_card()
    {
        var today = new DateTime(2026, 10, 9, 15, 0, 0);
        var weather = new WeatherData
        {
            Name = "Moscow", Country = "Russia", LocalTime = today,
            Current = new CurrentWeather { ConditionText = "солнечно" },
            HourlyForecast = [new HourlyForecast { Time = today.AddHours(1), ConditionText = "небольшой дождь" }],
            DailyForecast =
            [
                new DailyForecast { Date = today.Date, ConditionText = "солнечно" },
                new DailyForecast { Date = today.Date.AddDays(1), ConditionText = "облачно" },
                new DailyForecast { Date = today.Date.AddDays(2), ConditionText = "переменная облачность" }
            ]
        };
        var viewModel = new WeatherViewModel(new FakeRepository(() => Task.FromResult(weather)));

        await viewModel.LoadAsync();

        Assert.Equal("Солнечно", viewModel.Display!.Condition);
        Assert.Equal("Небольшой дождь", Assert.Single(viewModel.Display.Hourly).Condition);
        Assert.Equal(["Сегодня", "Завтра", "Воскресенье"], viewModel.Display.Daily.Select(day => day.Name));
        Assert.Equal(["Солнечно", "Облачно", "Переменная облачность"],
            viewModel.Display.Daily.Select(day => day.Condition));
    }

    [Fact]
    public async Task English_condition_is_capitalized_without_changing_other_words()
    {
        var viewModel = new WeatherViewModel(new FakeRepository(() => Task.FromResult(new WeatherData
        {
            Country = "Germany", Current = new CurrentWeather { ConditionText = "light rain" }
        })));

        await viewModel.LoadAsync();

        Assert.Equal("Light rain", viewModel.Display!.Condition);
    }

    private sealed class FakeRepository(Func<Task<WeatherData>> load) : IWeatherRepository
    {
        public Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default) => load();
    }
}
