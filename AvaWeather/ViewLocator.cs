using Avalonia.Controls;
using Avalonia.Controls.Templates;
using AvaWeather.ViewModels;
using AvaWeather.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AvaWeather;

public sealed class ViewLocator(IServiceProvider services) : IDataTemplate
{
    public bool Match(object? data) => data is WeatherViewModel;

    public Control Build(object? data) => data switch
    {
        WeatherViewModel => services.GetRequiredService<WeatherView>(),
        _ => new TextBlock { Text = "Weather view is unavailable." }
    };
}
