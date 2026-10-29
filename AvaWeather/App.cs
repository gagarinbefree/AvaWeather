using Application.Extensions;
using Application.Caching;
using Application.Interfaces;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using AvaWeather.Services;
using AvaWeather.ViewModels;
using AvaWeather.Views;
using Infrastructure.Configuration;
using Infrastructure.Caching;
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
            BaseUrl = settings["WeatherApi:BaseUrl"] ?? WeatherBackendConfiguration.BaseUrl,
            DefaultLocation = string.IsNullOrWhiteSpace(settings["WeatherApi:DefaultLocation"])
                ? "auto:ip" : settings["WeatherApi:DefaultLocation"]!,
            ForecastDays = int.TryParse(settings["WeatherApi:ForecastDays"], out var days) ? days : 3,
            UseDirectConnectionFallback = true
        };

        var registrations = new ServiceCollection();
        registrations.AddLogging();
#if DEBUG
        var apiLog = new Diagnostics.FileApiAccessLog(Path.Combine(AppContext.BaseDirectory, "logs", "api-access.log"));
        registrations.AddWeatherDiagnostics(apiLog);
        apiLog.Write(new ApiAccessEvent("WeatherServer", "configuration", "Configured", 0));
#else
        registrations.AddWeatherDiagnostics();
#endif
        registrations.AddApplication();
        registrations.AddSingleton<IWeatherCacheStorage>(new FileWeatherCacheStorage(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AvaWeather", "weather-cache.json")));
        registrations.AddInfrastructure(options);
        registrations.AddWeatherFailureReporting(
            WeatherTelemetryConfiguration.Resolve(settings["Telemetry:Dsn"]));
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
