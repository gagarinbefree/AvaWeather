using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaWeather.Services;
using AvaWeather.ViewModels;
using AvaWeather.Views;
using Domain.Entities;

[assembly: AvaloniaTestApplication(typeof(AvaWeather.Tests.HeadlessAppBuilder))]

namespace AvaWeather.Tests;

public class HeadlessAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<HeadlessApp>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

public sealed class HeadlessApp : Avalonia.Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
}

public class WeatherViewTests
{
    [AvaloniaFact]
    public async Task Dashboard_renders_weather_cards_with_compiled_bindings()
    {
        var now = new DateTime(2026, 10, 9, 15, 30, 0);
        var data = new WeatherData
        {
            Name = "Moscow", Country = "Russia", Region = "Moscow", LocalTime = now,
            Current = new CurrentWeather { TempC = 12, FeelslikeC = 10, ConditionText = "Sunny", ConditionCode = 1000, IsDay = 1,
                Humidity = 65, WindKph = 10.5, WindDir = "N", PressureMb = 1012, Uv = 3, VisKm = 10, LastUpdated = now },
            HourlyForecast = Enumerable.Range(16, 8).Select(hour => new HourlyForecast
            {
                Time = now.Date.AddHours(hour), TempC = 13 + hour - 16,
                ConditionText = "Sunny", ConditionCode = 1000, IsDay = 1, ChanceOfRain = 10
            }).ToList(),
            DailyForecast = Enumerable.Range(0, 3).Select(day => new DailyForecast
            {
                Date = now.Date.AddDays(day), MaxtempC = 16 + day, MintempC = 8 + day,
                ConditionText = "Sunny", ConditionCode = 1000, Avghumidity = 60,
                MaxwindKph = 12, DailyChanceOfRain = 10, Uv = 4, Sunrise = "06:30 AM", Sunset = "07:00 PM"
            }).ToList()
        };
        var vm = new WeatherViewModel(new FakeRepository(data));
        await vm.LoadAsync();
        var window = new Window { Width = 1100, Height = 750, Content = new WeatherView { DataContext = vm } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();

        var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(x => x.Text).ToArray();
        Assert.Contains("Moscow, Russia", texts);
        Assert.Contains("Hourly Forecast", string.Join(' ', texts));
        Assert.Contains("3-Day Forecast", string.Join(' ', texts));
        Assert.Contains("12°C", texts);
        Assert.Contains("Today", texts);
        Assert.Contains("Tomorrow", texts);

        var output = Environment.GetEnvironmentVariable("AVAWEATHER_SCREENSHOT");
        if (!string.IsNullOrWhiteSpace(output))
            window.CaptureRenderedFrame()?.Save(output, PngBitmapEncoderOptions.Default);
        window.Close();

        var narrow = new Window { Width = 560, Height = 780, Content = new WeatherView { DataContext = vm } };
        narrow.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        var narrowOutput = Environment.GetEnvironmentVariable("AVAWEATHER_NARROW_SCREENSHOT");
        if (!string.IsNullOrWhiteSpace(narrowOutput))
            narrow.CaptureRenderedFrame()?.Save(narrowOutput, PngBitmapEncoderOptions.Default);
        narrow.Close();
    }

    private sealed class FakeRepository(WeatherData weather) : IWeatherRepository
    {
        public Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default) => Task.FromResult(weather);
    }

    [AvaloniaFact]
    public async Task Retry_button_runs_view_model_command()
    {
        var calls = 0;
        var weather = new WeatherData { Name = "Moscow", Current = new CurrentWeather() };
        var vm = new WeatherViewModel(new DelegateRepository(() => ++calls == 1
            ? Task.FromException<WeatherData>(new HttpRequestException("offline"))
            : Task.FromResult(weather)));
        await vm.LoadAsync();
        var window = new Window { Width = 800, Height = 600, Content = new WeatherView { DataContext = vm } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var retry = window.GetVisualDescendants().OfType<Button>().Single();
        Assert.Same(vm.LoadCommand, retry.Command);
        await vm.LoadCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, calls);
        Assert.True(vm.HasWeather);
        Assert.False(vm.HasError);
        window.Close();
    }

    private sealed class DelegateRepository(Func<Task<WeatherData>> load) : IWeatherRepository
    {
        public Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default) => load();
    }
}
