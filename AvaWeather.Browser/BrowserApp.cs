using Application.Extensions;
using Application.Interfaces;
using Application.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using AvaWeather.ViewModels;
using AvaWeather.Views;
using Infrastructure.Extensions;
using Infrastructure.Clients;
using Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AvaWeather.Browser;

public sealed class BrowserApp : Avalonia.Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var registrations = new ServiceCollection();
        registrations.AddLogging();
        registrations.AddWeatherDiagnostics();
        registrations.AddApplication();
        registrations.AddInfrastructure(Program.Options);
        registrations.AddWeatherFailureReporting(
            WeatherTelemetryConfiguration.Resolve(Program.TelemetryDsn));
        registrations.AddTransient<IDeviceLocationClient, BrowserDeviceLocationClient>();
        registrations.AddTransient<IIpLocationClient>(provider => new DeviceFirstLocationClient(
            provider.GetRequiredService<IDeviceLocationClient>(),
            provider.GetRequiredService<GeoJsLocationClient>()));
        registrations.AddTransient<WeatherViewModel>();
        registrations.AddTransient<WeatherView>(_ => new WeatherView(singleViewMode: true));
        _services = registrations.BuildServiceProvider();
        DataTemplates.Add(new ViewLocator(_services));

        if (ApplicationLifetime is ISingleViewApplicationLifetime browser)
        {
            var viewModel = _services.GetRequiredService<WeatherViewModel>();
            viewModel.PropertyChanged += (_, change) =>
            {
                if (change.PropertyName == nameof(WeatherViewModel.IsLoading))
                    BrowserAccessibility.Publish(viewModel);
            };
            browser.MainView = new ContentControl { Content = viewModel };
            BrowserAccessibility.Publish(viewModel);
            _ = viewModel.LoadAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
