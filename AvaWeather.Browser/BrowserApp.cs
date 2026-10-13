using Application.Extensions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using AvaWeather.ViewModels;
using AvaWeather.Views;
using Infrastructure.Extensions;
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
        registrations.AddApplication();
        registrations.AddInfrastructure(Program.Options);
        registrations.AddTransient<WeatherViewModel>();
        registrations.AddTransient<WeatherView>(_ => new WeatherView(singleViewMode: true));
        _services = registrations.BuildServiceProvider();
        DataTemplates.Add(new ViewLocator(_services));

        if (ApplicationLifetime is ISingleViewApplicationLifetime browser)
        {
            var viewModel = _services.GetRequiredService<WeatherViewModel>();
            browser.MainView = new ContentControl { Content = viewModel };
            _ = viewModel.LoadAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
