using Application.Interfaces;
using AvaWeather.ViewModels;
using Domain.Entities;

namespace Tests;

public class AccessibleForecastTests
{
    [Theory]
    [InlineData("Russia", "ru", "Текущая погода", "Текстовый прогноз")]
    [InlineData("Germany", "en", "Current weather", "Text forecast")]
    public async Task Ready_snapshot_uses_the_same_localized_values_as_the_canvas(
        string country, string language, string currentHeading, string linkLabel)
    {
        var time = new DateTime(2030, 4, 20, 15, 0, 0);
        var viewModel = new WeatherViewModel(new Repository(() => Task.FromResult(new WeatherData
        {
            Name = "Test City", Country = country, LocalTime = time,
            Current = new CurrentWeather { TempC = 12.5, ConditionText = "Cloudy", Humidity = 66 },
            HourlyForecast = [new HourlyForecast { Time = time.AddHours(1), TempC = 11, ConditionText = "Rain", ChanceOfRain = 42 }],
            DailyForecast = [new DailyForecast { Date = time.Date, MaxtempC = 15, MintempC = 8, ConditionText = "Cloudy" }]
        })));

        await viewModel.LoadAsync();
        var snapshot = AccessibleForecast.From(viewModel);

        Assert.Equal(language, snapshot.Language);
        Assert.Equal(linkLabel, snapshot.LinkLabel);
        Assert.Equal(currentHeading, snapshot.CurrentHeading);
        Assert.Equal(viewModel.Display!.Location, snapshot.Heading);
        Assert.Equal(viewModel.Display.Temperature, snapshot.Temperature);
        Assert.Equal(viewModel.Display.Condition, snapshot.Condition);
        Assert.Equal(viewModel.Display.Humidity, snapshot.CurrentMetrics.Single(x => x.Label == viewModel.Strings.Humidity).Value);
        Assert.Equal(viewModel.Display.Hourly[0].Temperature, Assert.Single(snapshot.Hourly).Temperature);
        Assert.Equal(viewModel.Display.Hourly[0].Rain, Assert.Single(snapshot.Hourly).Rain);
        Assert.Equal(viewModel.Display.Daily[0].Maximum, Assert.Single(snapshot.Daily).Maximum);
        Assert.Equal(viewModel.Display.Daily[0].SunriseSunset, Assert.Single(snapshot.Daily).SunriseSunset);
    }

    [Fact]
    public async Task Loading_and_error_snapshots_have_readable_status_and_retry()
    {
        var response = new TaskCompletionSource<WeatherData>();
        var viewModel = new WeatherViewModel(new Repository(() => response.Task));
        var initial = AccessibleForecast.From(viewModel);
        Assert.Equal(viewModel.Strings.LoadingWeather, initial.Status);

        var loading = viewModel.LoadAsync();
        Assert.True(AccessibleForecast.From(viewModel).IsLoading);

        response.SetException(new HttpRequestException("offline"));
        await loading;
        var error = AccessibleForecast.From(viewModel);
        Assert.False(error.IsLoading);
        Assert.Equal(viewModel.ErrorMessage, error.Status);
        Assert.Equal(viewModel.Strings.Retry, error.RetryLabel);
    }

    private sealed class Repository(Func<Task<WeatherData>> load) : IWeatherRepository
    {
        public Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default) => load();
    }
}
