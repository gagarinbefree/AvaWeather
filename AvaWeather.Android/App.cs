using Application.Extensions;
using Application.Interfaces;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using AvaWeather.Services;
#if DEBUG
using AvaWeather.Diagnostics;
#endif
using AvaWeather.ViewModels;
using AvaWeather.Views;
using Infrastructure.Configuration;
using Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AvaWeather.Android;

public sealed class App : Avalonia.Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var settings = new ConfigurationBuilder();
#if DEBUG
        using (var resource = typeof(App).Assembly.GetManifestResourceStream(
                   "AvaWeather.Android.appsettings.Development.json"))
        {
            if (resource is not null)
                settings.AddJsonStream(resource);
        }
#endif
        var config = settings.Build();
        var options = new WeatherApiOptions
        {
            BaseUrl = "https://api.weatherapi.com/v1/",
            ApiKey = WeatherApiKeyResolver.Resolve(
                Environment.GetEnvironmentVariable("WEATHER_API_KEY"),
                config["WeatherApi:ApiKey"], EmbeddedWeatherApiKey.Read),
            DefaultLocation = "auto:ip",
            ForecastDays = 3,
            UseDirectConnectionFallback = true
        };

        var registrations = new ServiceCollection();
        registrations.AddLogging();
#if DEBUG
        var logDirectory = global::Android.App.Application.Context.FilesDir?.AbsolutePath
            ?? AppContext.BaseDirectory;
        registrations.AddSingleton<IApiAccessLog>(
            new FileApiAccessLog(Path.Combine(logDirectory, "logs", "api-access.log")));
#endif
        registrations.AddApplication();
        registrations.AddInfrastructure(options);
        registrations.AddTransient<WeatherViewModel>();
        registrations.AddTransient<WeatherView>(_ => new WeatherView(singleViewMode: true));
        _services = registrations.BuildServiceProvider();
        DataTemplates.Add(new ViewLocator(_services));

        if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            activity.MainViewFactory = () =>
            {
                var viewModel = _services.GetRequiredService<WeatherViewModel>();
                var view = new ContentControl { Content = viewModel };
                _ = viewModel.LoadAsync();
                return view;
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
