using Application.Extensions;
using Application.Interfaces;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using AvaWeather.Services;
using AvaWeather.ViewModels;
using AvaWeather.Theming;
using AvaWeather.Views;
using Infrastructure.Configuration;
using Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AvaWeather;

public sealed class App : Avalonia.Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var settings = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
        var options = new WeatherApiOptions
        {
            BaseUrl = settings["WeatherApi:BaseUrl"] ?? "https://api.weatherapi.com/v1/",
            ApiKey = WeatherApiKeyResolver.Resolve(
                Environment.GetEnvironmentVariable("WEATHER_API_KEY"),
                settings["WeatherApi:ApiKey"], EmbeddedWeatherApiKey.Read),
            DefaultLocation = string.IsNullOrWhiteSpace(settings["WeatherApi:DefaultLocation"])
                ? "auto:ip" : settings["WeatherApi:DefaultLocation"]!,
            ForecastDays = int.TryParse(settings["WeatherApi:ForecastDays"], out var days) ? days : 3
        };

        var registrations = new ServiceCollection();
        registrations.AddLogging();
#if DEBUG
        var apiLog = new Diagnostics.FileApiAccessLog(Path.Combine(AppContext.BaseDirectory, "logs", "api-access.log"));
        registrations.AddSingleton<IApiAccessLog>(apiLog);
        var source = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEATHER_API_KEY"))
            ? "environment" : !string.IsNullOrWhiteSpace(settings["WeatherApi:ApiKey"])
                ? "appsettings" : string.IsNullOrWhiteSpace(options.ApiKey) ? "missing" : "embedded";
        apiLog.Write(new ApiAccessEvent("WeatherAPI", "configuration",
            string.IsNullOrWhiteSpace(options.ApiKey) ? "MissingKey" : "KeyAvailable", 0,
            CredentialSource: source));
#endif
        registrations.AddSingleton(ThemeColorService.Default);
        registrations.AddApplication();
        registrations.AddInfrastructure(options);
        registrations.AddTransient<IWeatherRepository, WeatherRepository>();
        registrations.AddHttpClient<IIpLocationClient, GeoJsLocationClient>(client =>
        {
            client.BaseAddress = new Uri("https://get.geojs.io/");
            client.Timeout = TimeSpan.FromSeconds(4);
        });
        registrations.AddHttpClient<IPlaceNameLocalizer, OpenMeteoPlaceNameLocalizer>(client =>
        {
            client.BaseAddress = new Uri("https://geocoding-api.open-meteo.com/");
            client.Timeout = TimeSpan.FromSeconds(3);
        });
        registrations.AddTransient<WeatherViewModel>();
        registrations.AddTransient<WeatherView>();
        _services = registrations.BuildServiceProvider();
        DataTemplates.Add(new ViewLocator(_services));

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = _services.GetRequiredService<WeatherViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = viewModel, Content = viewModel };
            desktop.MainWindow.Opened += async (_, _) =>
            {
                MacDockIcon.Apply();
                await viewModel.LoadAsync();
            };
            desktop.Exit += (_, _) => _services.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
