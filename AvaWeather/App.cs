using Application.Extensions;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using AvaWeather.Services;
using AvaWeather.ViewModels;
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
            ApiKey = Environment.GetEnvironmentVariable("WEATHER_API_KEY") ?? settings["WeatherApi:ApiKey"] ?? "",
            DefaultLocation = string.IsNullOrWhiteSpace(settings["WeatherApi:DefaultLocation"])
                ? "auto:ip" : settings["WeatherApi:DefaultLocation"]!,
            ForecastDays = int.TryParse(settings["WeatherApi:ForecastDays"], out var days) ? days : 3
        };

        var registrations = new ServiceCollection();
        registrations.AddLogging();
        registrations.AddApplication();
        registrations.AddInfrastructure(options);
        registrations.AddTransient<IWeatherRepository, WeatherRepository>();
        registrations.AddTransient<WeatherViewModel>();
        registrations.AddTransient<WeatherView>();
        _services = registrations.BuildServiceProvider();
        DataTemplates.Add(new ViewLocator(_services));

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = _services.GetRequiredService<WeatherViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = viewModel, Content = viewModel };
            desktop.MainWindow.Opened += async (_, _) => await viewModel.LoadAsync();
            desktop.Exit += (_, _) => _services.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
