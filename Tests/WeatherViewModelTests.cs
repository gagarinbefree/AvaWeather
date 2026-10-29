using Application.Interfaces;
using Application.Diagnostics;
using AvaWeather.ViewModels;
using Domain.Entities;
using System.Net;
using Application.Caching;

namespace Tests;

public class WeatherViewModelTests
{
    [Fact]
    public async Task Server_retained_weather_keeps_saved_status_and_original_cache_time()
    {
        var original = new DateTimeOffset(2026, 10, 28, 4, 0, 0, TimeSpan.Zero);
        var weather = new WeatherData { Name = "Perm", Country = "Russia", Current = new CurrentWeather(), ServerCachedAtUtc = original };
        var cache = new FakeCache();
        var model = new WeatherViewModel(new FakeRepository(() => Task.FromResult(weather)), cache: cache);
        await model.LoadAsync();
        Assert.True(model.IsCachedWeather);
        Assert.NotEmpty(model.CacheStatus);
        Assert.Equal(original, cache.FetchedAt);
    }

    [Fact]
    public async Task Cached_weather_is_visible_while_request_runs_and_replaced_after_success()
    {
        var old = new WeatherData
        {
            Name = "Perm",
            Country = "Russia",
            LocalTime = new DateTime(2026, 10, 19, 12, 0, 0),
            Current = new CurrentWeather { TempC = 8 },
            HourlyForecast = [new HourlyForecast { Time = new DateTime(2026, 10, 19, 12, 0, 0) }],
            DailyForecast = [new DailyForecast { Date = new DateTime(2026, 10, 19) }]
        };
        var fresh = new WeatherData { Name = "Berlin", Country = "Germany", Current = new CurrentWeather { TempC = 12 } };
        var response = new TaskCompletionSource<WeatherData>();
        var cache = new FakeCache { Cached = new CachedWeather(1, new DateTimeOffset(2026, 10, 19, 7, 0, 0, TimeSpan.Zero), "ru", old) };
        var model = new WeatherViewModel(new FakeRepository(() => response.Task), cache: cache);

        var load = model.LoadAsync();
        Assert.True(model.IsLoading);
        Assert.True(model.HasWeather);
        Assert.True(model.IsCachedWeather);
        Assert.Contains("19", model.CacheStatus);
        Assert.False(model.Display!.Hourly[0].IsNow);
        Assert.Contains("19", model.Display.Hourly[0].Time);
        Assert.DoesNotContain("Сегодня", model.HourlySubtitle);
        Assert.NotEqual(model.Strings.Today, model.Display.Daily[0].Name);
        Assert.Equal("Perm", model.Weather!.Name);
        Assert.Equal(model.CacheStatus, AccessibleForecast.From(model).Updated);

        response.SetResult(fresh);
        await load;
        Assert.Same(fresh, model.Weather);
        Assert.False(model.IsCachedWeather);
        Assert.Equal(model.Strings.TodayTomorrow, model.HourlySubtitle);
        Assert.Same(fresh, cache.Saved);
        Assert.Equal("en", cache.Language);
    }

    [Fact]
    public async Task Cache_storage_failure_does_not_block_weather_loading()
    {
        var fresh = new WeatherData { Name = "Perm", Current = new CurrentWeather() };
        var cache = new FakeCache { Fail = true };
        var model = new WeatherViewModel(new FakeRepository(() => Task.FromResult(fresh)), cache: cache);
        await model.LoadAsync();
        Assert.Same(fresh, model.Weather);
        Assert.Null(model.ErrorMessage);
    }

    [Fact]
    public async Task Cached_weather_remains_visible_if_refresh_fails()
    {
        var cached = new WeatherData { Name = "Perm", Country = "Russia", Current = new CurrentWeather() };
        var cache = new FakeCache { Cached = new CachedWeather(1, DateTimeOffset.UtcNow, "ru", cached) };
        var model = new WeatherViewModel(new FakeRepository(() => Task.FromException<WeatherData>(new HttpRequestException())), cache: cache);
        await model.LoadAsync();
        Assert.True(model.HasWeather);
        Assert.True(model.HasError);
        Assert.True(model.IsCachedWeather);
        Assert.Same(cached, model.Weather);
    }

    [Fact]
    public async Task Failed_retry_marks_previous_success_as_saved_and_keeps_its_location()
    {
        var calls = 0;
        var fresh = new WeatherData { Name = "Perm", Country = "Russia", Current = new CurrentWeather() };
        var model = new WeatherViewModel(new FakeRepository(() => ++calls == 1
            ? Task.FromResult(fresh) : Task.FromException<WeatherData>(new HttpRequestException())));
        await model.LoadAsync();
        await model.LoadAsync();
        Assert.True(model.HasWeather);
        Assert.True(model.HasError);
        Assert.True(model.IsCachedWeather);
        Assert.Contains("Сохранённый прогноз", model.CacheStatus);
        Assert.Equal("Perm", model.HeaderLocation);
    }

    [Fact]
    public async Task Retry_after_failed_refresh_replaces_saved_city()
    {
        var calls = 0;
        var old = new WeatherData { Name = "Perm", Country = "Russia", Current = new CurrentWeather() };
        var fresh = new WeatherData { Name = "Berlin", Country = "Germany", Current = new CurrentWeather() };
        var cache = new FakeCache { Cached = new CachedWeather(1, DateTimeOffset.UtcNow, "ru", old) };
        var model = new WeatherViewModel(new FakeRepository(() => ++calls == 1
            ? Task.FromException<WeatherData>(new HttpRequestException()) : Task.FromResult(fresh)), cache: cache);
        await model.LoadAsync();
        Assert.Equal("Perm", model.HeaderLocation);
        await model.LoadAsync();
        Assert.Equal("Berlin", model.HeaderLocation);
        Assert.False(model.HasError);
        Assert.False(model.IsCachedWeather);
        Assert.Equal("en-US", model.Strings.Culture.Name);
        Assert.Same(fresh, cache.Saved);
    }

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
            Name = "Kyiv",
            Country = "Ukraine",
            LocalTime = date,
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
            Name = "Berlin",
            Country = "Germany",
            Current = new CurrentWeather { TempC = 12.5 }
        })));

        await viewModel.LoadAsync();

        Assert.Equal("en-US", viewModel.Strings.Culture.Name);
        Assert.Equal("Hourly Forecast", viewModel.Strings.HourlyForecast);
        Assert.Equal("Berlin, Germany", viewModel.Display!.Location);
        Assert.Equal("12.5°C", viewModel.Display.Temperature);
    }

    [Fact]
    public async Task Timed_out_load_shows_connection_error_and_allows_retry()
    {
        var calls = 0;
        var viewModel = new WeatherViewModel(new FakeRepository(() =>
        {
            calls++;
            return calls == 1
                ? Task.FromException<WeatherData>(new TaskCanceledException("request timed out"))
                : Task.FromResult(new WeatherData { Current = new CurrentWeather() });
        }));

        await viewModel.LoadAsync();
        Assert.True(viewModel.HasError);
        Assert.Contains("internet connection", viewModel.ErrorMessage);

        await viewModel.LoadAsync();
        Assert.True(viewModel.HasWeather);
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
    public async Task Final_failure_is_reported_once_and_reporting_cannot_break_retry()
    {
        var reporter = new RecordingReporter(throwOnReport: true);
        var calls = 0;
        var viewModel = new WeatherViewModel(new FakeRepository(() =>
            ++calls == 1
                ? Task.FromException<WeatherData>(new HttpRequestException("offline"))
                : Task.FromResult(new WeatherData { Current = new CurrentWeather() })),
            failureReporter: reporter);

        await viewModel.LoadAsync();
        Assert.Equal([WeatherFailureKind.Connection], reporter.Failures);
        Assert.True(viewModel.HasError);

        await viewModel.LoadAsync();
        Assert.True(viewModel.HasWeather);
        Assert.Single(reporter.Failures);
    }

    [Fact]
    public async Task Russian_day_names_and_conditions_start_with_capital_letters_on_every_card()
    {
        var today = new DateTime(2026, 10, 9, 15, 0, 0);
        var weather = new WeatherData
        {
            Name = "Moscow",
            Country = "Russia",
            LocalTime = today,
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
            Country = "Germany",
            Current = new CurrentWeather { ConditionText = "light rain" }
        })));

        await viewModel.LoadAsync();

        Assert.Equal("Light rain", viewModel.Display!.Condition);
    }

    private sealed class FakeRepository(Func<Task<WeatherData>> load) : IWeatherRepository
    {
        public Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default) => load();
    }

    private sealed class FakeCache : IWeatherCache
    {
        public CachedWeather? Cached { get; init; }
        public bool Fail { get; init; }
        public WeatherData? Saved { get; private set; }
        public string? Language { get; private set; }
        public DateTimeOffset? FetchedAt { get; private set; }
        public Task<CachedWeather?> LoadAsync(CancellationToken cancellationToken = default) =>
            Fail ? Task.FromException<CachedWeather?>(new IOException()) : Task.FromResult(Cached);
        public Task SaveAsync(WeatherData weather, DateTimeOffset fetchedAt, string language, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new IOException();
            Saved = weather;
            FetchedAt = fetchedAt;
            Language = language;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingReporter(bool throwOnReport = false) : IWeatherFailureReporter
    {
        public List<WeatherFailureKind> Failures { get; } = [];
        public void Report(WeatherFailureKind failure)
        {
            Failures.Add(failure);
            if (throwOnReport) throw new InvalidOperationException("telemetry unavailable");
        }
    }
}
