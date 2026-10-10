using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Text;
using AvaWeather.Services;
using AvaWeather.ViewModels;
using AvaWeather.Views;
using Domain.Entities;
using FluentIcons.Avalonia;
using FluentIcons.Common;

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
    public void Main_window_uses_branded_icon()
    {
        var window = new MainWindow();
        Assert.NotNull(window.Icon);
    }

    [AvaloniaFact]
    public void Main_window_uses_custom_draggable_header_and_close_button()
    {
        var window = new MainWindow { Content = new WeatherView() };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(WindowDecorations.None, window.WindowDecorations);
        Assert.True(window.ExtendClientAreaToDecorationsHint);
        Assert.True(window.CanResize);
        Assert.Equal(820, window.Height);
        var header = window.GetVisualDescendants().OfType<Border>()
            .Single(border => border.Name == "WeatherHeader");
        Assert.Equal(WindowDecorationsElementRole.TitleBar,
            WindowDecorationProperties.GetElementRole(header));
        var close = window.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Name == "CloseWindowButton");
        Assert.Equal(WindowDecorationsElementRole.User,
            WindowDecorationProperties.GetElementRole(close));

        close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.False(window.IsVisible);
    }

    [AvaloniaFact]
    public void Borderless_window_has_resize_targets_on_every_edge_and_corner()
    {
        var window = new MainWindow { Content = new WeatherView() };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var roles = window.GetVisualDescendants().OfType<Border>()
            .Select(WindowDecorationProperties.GetElementRole)
            .ToArray();
        foreach (var role in new[]
        {
            WindowDecorationsElementRole.ResizeN, WindowDecorationsElementRole.ResizeS,
            WindowDecorationsElementRole.ResizeE, WindowDecorationsElementRole.ResizeW,
            WindowDecorationsElementRole.ResizeNE, WindowDecorationsElementRole.ResizeNW,
            WindowDecorationsElementRole.ResizeSE, WindowDecorationsElementRole.ResizeSW
        })
            Assert.Contains(role, roles);

        window.Close();
    }

    [AvaloniaFact]
    public async Task Header_gradient_follows_current_weather_without_background_icons()
    {
        var calls = 0;
        var vm = new WeatherViewModel(new DelegateRepository(() => Task.FromResult(new WeatherData
        {
            Name = "Perm", Country = "Russia",
            Current = new CurrentWeather { ConditionCode = ++calls == 1 ? 1000 : 1183, IsDay = 1 }
        })));
        var window = new MainWindow { Content = new WeatherView { DataContext = vm } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var header = window.GetVisualDescendants().OfType<Border>()
            .Single(border => border.Name == "WeatherHeader");
        Assert.Empty(header.GetVisualDescendants().OfType<WeatherIcon>());
        Assert.NotNull(header.Background);

        await vm.LoadAsync();
        Dispatcher.UIThread.RunJobs();
        var sunny = Assert.IsAssignableFrom<IGradientBrush>(header.Background);
        Assert.Equal(Color.Parse("#FFF0B8"), sunny.GradientStops[0].Color);
        Assert.Equal(Color.Parse("#FFF8DF"), sunny.GradientStops[1].Color);

        await vm.LoadAsync();
        Dispatcher.UIThread.RunJobs();
        var rainy = Assert.IsAssignableFrom<IGradientBrush>(header.Background);
        Assert.Equal(Color.Parse("#D7EAF5"), rainy.GradientStops[0].Color);
        Assert.Equal(Color.Parse("#EBF5FA"), rainy.GradientStops[1].Color);
        Assert.Empty(header.GetVisualDescendants().OfType<WeatherIcon>());
        window.Close();
    }

    [AvaloniaFact]
    public async Task Initial_window_height_fits_the_full_weather_dashboard()
    {
        var vm = new WeatherViewModel(new FakeRepository(new WeatherData
        {
            Name = "Perm", Country = "Russia", Current = new CurrentWeather(),
            HourlyForecast = [new HourlyForecast()],
            DailyForecast = [new DailyForecast(), new DailyForecast(), new DailyForecast()]
        }));
        await vm.LoadAsync();
        var window = new MainWindow { Content = new WeatherView { DataContext = vm } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var scroll = window.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.True(scroll.Extent.Height <= scroll.Viewport.Height,
            $"Weather dashboard needs {scroll.Extent.Height} px but has {scroll.Viewport.Height} px.");

        window.Close();
    }

    [AvaloniaFact]
    public void Mac_dock_uses_icon_embedded_in_the_executable()
    {
        if (!OperatingSystem.IsMacOS()) return;
        Assert.True(MacDockIcon.Apply(out var error), error);
    }

    [Theory]
    [InlineData(WeatherConditionKind.ClearDay, "#FFF0B8", "#FFF8DF")]
    [InlineData(WeatherConditionKind.PartlyCloudy, "#F4EBD4", "#FBF6E9")]
    [InlineData(WeatherConditionKind.Cloudy, "#DFE8EF", "#F1F5F8")]
    [InlineData(WeatherConditionKind.Rain, "#D7EAF5", "#EBF5FA")]
    [InlineData(WeatherConditionKind.Snow, "#E7EEF8", "#F5F8FC")]
    [InlineData(WeatherConditionKind.Fog, "#E7EAE5", "#F3F5F1")]
    [InlineData(WeatherConditionKind.Thunder, "#E4DFF2", "#F2EFFA")]
    [InlineData(WeatherConditionKind.ClearNight, "#DCE5F5", "#EEF3FB")]
    [InlineData(WeatherConditionKind.PartlyCloudyNight, "#DCE5F5", "#EEF3FB")]
    public void Palette_covers_every_weather_condition(WeatherConditionKind condition, string current, string forecast)
    {
        var colors = WeatherCardPalette.For(condition);
        Assert.Equal(Color.Parse(current), Assert.IsAssignableFrom<ISolidColorBrush>(colors.CurrentBackground).Color);
        Assert.Equal(Color.Parse(forecast), Assert.IsAssignableFrom<ISolidColorBrush>(colors.ForecastBackground).Color);
        var gradient = Assert.IsAssignableFrom<IGradientBrush>(colors.HeaderBackground);
        Assert.Equal(Color.Parse(current), gradient.GradientStops[0].Color);
        Assert.Equal(Color.Parse(forecast), gradient.GradientStops[1].Color);
    }

    [AvaloniaFact]
    public async Task Weather_cards_use_their_own_condition_colors()
    {
        var now = new DateTime(2026, 10, 9, 15, 0, 0);
        var data = new WeatherData
        {
            Name = "Moscow", Country = "Russia", LocalTime = now,
            Current = new CurrentWeather { ConditionCode = 1000, IsDay = 1 },
            HourlyForecast = [new HourlyForecast { Time = now.AddHours(1), ConditionCode = 1180, IsDay = 1 }],
            DailyForecast = [new DailyForecast { Date = now.Date, ConditionCode = 1087 }]
        };
        var vm = new WeatherViewModel(new FakeRepository(data));
        await vm.LoadAsync();
        Assert.True(vm.HasWeather, vm.ErrorMessage);
        var window = new Window { Width = 1100, Height = 750, Content = new WeatherView { DataContext = vm } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();

        var backgrounds = window.GetVisualDescendants().OfType<Border>()
            .Select(border => border.Background).OfType<ISolidColorBrush>().Select(brush => brush.Color).ToArray();
        Assert.Contains(Color.Parse("#FFF0B8"), backgrounds);
        Assert.Contains(Color.Parse("#EBF5FA"), backgrounds);
        Assert.Contains(Color.Parse("#F2EFFA"), backgrounds);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Dashboard_renders_weather_cards_with_compiled_bindings()
    {
        var now = new DateTime(2026, 10, 9, 15, 30, 0);
        var data = new WeatherData
        {
            Name = "Москва", Country = "Russia", Region = "Москва", LocalTime = now,
            Current = new CurrentWeather { TempC = 12, FeelslikeC = 10, ConditionText = "Солнечно", ConditionCode = 1000, IsDay = 1,
                Humidity = 65, WindKph = 10.5, WindDir = "N", PressureMb = 1012, Uv = 3, VisKm = 10, LastUpdated = now },
            HourlyForecast = Enumerable.Range(16, 8).Select(hour => new HourlyForecast
            {
                Time = now.Date.AddHours(hour), TempC = 13 + hour - 16,
                ConditionText = hour is >= 19 and <= 21 ? "Дождь" : hour >= 22 ? "Ясно" : "Солнечно",
                ConditionCode = hour is >= 19 and <= 21 ? 1180 : 1000,
                IsDay = hour >= 22 ? 0 : 1, ChanceOfRain = hour is >= 19 and <= 21 ? 75 : 10
            }).ToList(),
            DailyForecast = Enumerable.Range(0, 3).Select(day => new DailyForecast
            {
                Date = now.Date.AddDays(day), MaxtempC = 16 + day, MintempC = 8 + day,
                ConditionText = day == 0 ? "Солнечно" : day == 1 ? "Облачно" : "Гроза",
                ConditionCode = day == 0 ? 1000 : day == 1 ? 1006 : 1087, Avghumidity = 60,
                MaxwindKph = 12, DailyChanceOfRain = 10, Uv = 4, Sunrise = "06:30 AM", Sunset = "07:00 PM"
            }).ToList()
        };
        var vm = new WeatherViewModel(new FakeRepository(data));
        await vm.LoadAsync();
        var window = new MainWindow { Width = 1100, Content = new WeatherView { DataContext = vm } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();

        var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(x => x.Text).ToArray();
        Assert.Contains("Москва, Россия", texts);
        Assert.Contains("Почасовой прогноз", texts);
        Assert.Contains("Прогноз на 3 дня", texts);
        Assert.Contains("Ощущается как", texts);
        Assert.Contains("Восход / Закат", texts);
        Assert.Contains("Солнечно", texts);
        Assert.Contains("12°C", texts);
        Assert.Contains("Сегодня", texts);
        Assert.Contains("Завтра", texts);

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

        var retry = window.GetVisualDescendants().OfType<Button>()
            .Single(button => ReferenceEquals(button.Command, vm.LoadCommand));
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

    [AvaloniaFact]
    public async Task Visible_dashboard_changes_language_after_location_is_detected()
    {
        var response = new TaskCompletionSource<WeatherData>();
        var vm = new WeatherViewModel(new DelegateRepository(() => response.Task));
        var window = new Window { Width = 1000, Height = 700, Content = new WeatherView { DataContext = vm } };
        window.Show();
        var load = vm.LoadAsync();
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), block => block.Text == "Loading weather...");

        response.SetResult(new WeatherData
        {
            Name = "Almaty", Country = "Kazakhstan", Current = new CurrentWeather(),
            DailyForecast = [new DailyForecast()]
        });
        await load;
        Dispatcher.UIThread.RunJobs();

        var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text).ToArray();
        Assert.Contains("Почасовой прогноз", texts);
        Assert.Contains("Прогноз на 3 дня", texts);
        Assert.Contains("Almaty, Казахстан", texts);
        Assert.DoesNotContain("Hourly Forecast", texts);
        window.Close();
    }

    [AvaloniaFact]
    public async Task English_country_renders_english_dashboard()
    {
        var vm = new WeatherViewModel(new FakeRepository(new WeatherData
        {
            Name = "Berlin", Country = "Germany", Current = new CurrentWeather()
        }));
        await vm.LoadAsync();
        var window = new Window { Width = 1000, Height = 700, Content = new WeatherView { DataContext = vm } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text).ToArray();
        Assert.Contains("Hourly Forecast", texts);
        Assert.Contains("3-Day Forecast", texts);
        Assert.Contains("Berlin, Germany", texts);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Header_shows_location_returned_by_weather_service()
    {
        var vm = new WeatherViewModel(new FakeRepository(new WeatherData
        {
            Name = "Perm", Country = "Russia", Current = new CurrentWeather()
        }));
        await vm.LoadAsync();
        var window = new Window { Width = 900, Height = 600, Content = new WeatherView { DataContext = vm } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var texts = window.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text).ToArray();
        Assert.Contains("Perm", texts);
        Assert.DoesNotContain("Moscow", texts);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Dashboard_uses_icon_pack_controls_instead_of_text_symbols()
    {
        var weather = new WeatherData
        {
            Name = "Moscow", Country = "Russia", LocalTime = new DateTime(2026, 10, 9, 15, 0, 0),
            Current = new CurrentWeather { ConditionCode = 1000, IsDay = 1 },
            HourlyForecast = [new HourlyForecast { ConditionCode = 1000, IsDay = 1 }],
            DailyForecast = [new DailyForecast { ConditionCode = 1000 }]
        };
        var vm = new WeatherViewModel(new FakeRepository(weather));
        await vm.LoadAsync();
        var window = new Window { Width = 1100, Height = 750, Content = new WeatherView { DataContext = vm } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var controls = window.GetVisualDescendants().ToArray();
        var icons = controls.OfType<FluentIcon>().ToArray();
        Assert.True(icons.Length >= 12);
        Assert.Contains(icons, icon => icon.Icon == Icon.WeatherSunny);
        Assert.Contains(icons, icon => icon.Icon == Icon.WeatherRain);
        Assert.DoesNotContain(controls.OfType<TextBlock>(), block =>
            block.Text?.EnumerateRunes().Any(rune =>
                rune.Value is >= 0x2600 and <= 0x27BF or >= 0x1F300 and <= 0x1FAFF) == true);
        window.Close();
    }

    [AvaloniaFact]
    public void Weather_icon_always_uses_pack_icon_for_conditions()
    {
        var icon = new WeatherIcon(40);
        Assert.Empty(icon.Children.OfType<Image>());
        var glyph = Assert.Single(icon.Children.OfType<FluentIcon>());
        Assert.Equal(Icon.WeatherSunny, glyph.Icon);
        icon.Kind = WeatherConditionKind.Rain;
        Assert.Equal(Icon.WeatherRain, glyph.Icon);
        icon.Kind = WeatherCondition.For(1003, false);
        Assert.Equal(Icon.WeatherPartlyCloudyNight, glyph.Icon);
        Assert.True(glyph.IsVisible);
    }
}
