using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Application.Extensions;
using AvaWeather.Services;
using AvaWeather.ViewModels;
using AvaWeather.Widgets;
using Infrastructure.Configuration;
using Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Windows.Widgets.Providers;
using Weather.Localization;
using AvaWeather.Theming;

namespace AvaWeather.Widget.Windows;

[ComVisible(true)]
[ComDefaultInterface(typeof(IWidgetProvider))]
[Guid("54CFD63A-C395-4857-971A-75469EB72519")]
public sealed class WeatherWidgetProvider : IWidgetProvider
{
    private static readonly ConcurrentDictionary<string, byte> Widgets = new();
    private static readonly Lazy<ServiceProvider> Services = new(BuildServices);
    private static readonly SemaphoreSlim RefreshGate = new(1, 1);
    private static readonly Timer RefreshTimer = new(_ => _ = RefreshAsync(), null,
        TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(20));

    public WeatherWidgetProvider()
    {
        _ = RefreshTimer;
        foreach (var info in WidgetManager.GetDefault().GetWidgetInfos())
        {
            if (info.WidgetContext.DefinitionId == "AvaWeather.Current")
                Widgets.TryAdd(info.WidgetContext.Id, 0);
        }
        _ = RefreshAsync();
    }

    public void CreateWidget(WidgetContext context)
    {
        Widgets.TryAdd(context.Id, 0);
        Update(context.Id, new WidgetSnapshot("AvaWeather", "--°", "", StringLocalizer.Current.Get("LoadingWeather"), "",
            ThemeColorService.Default.GetHex("Weather.Cloudy.Current"),
            ThemeColorService.Default.GetHex("Weather.Cloudy.Accent"),
            WeatherCondition.WidgetSymbolFor(AvaWeather.ViewModels.WeatherConditionKind.Cloudy)));
        _ = RefreshAsync();
    }

    public void DeleteWidget(string widgetId, string customState) => Widgets.TryRemove(widgetId, out _);
    public void Activate(WidgetContext context) { Widgets.TryAdd(context.Id, 0); _ = RefreshAsync(); }
    public void Deactivate(string widgetId) { }
    public void OnWidgetContextChanged(WidgetContextChangedArgs args) => _ = RefreshAsync();
    public void OnActionInvoked(WidgetActionInvokedArgs args) => _ = RefreshAsync();

    private static async Task RefreshAsync()
    {
        if (!await RefreshGate.WaitAsync(0)) return;
        try
        {
            using var scope = Services.Value.CreateScope();
            var weather = await scope.ServiceProvider.GetRequiredService<IWeatherRepository>().GetWeatherAsync();
            var snapshot = WidgetSnapshot.From(weather);
            foreach (var widgetId in Widgets.Keys) Update(widgetId, snapshot);
        }
        catch (Exception error)
        {
            System.Diagnostics.Trace.WriteLine(StringLocalizer.Current.Format("WidgetRefreshFailed", error));
        }
        finally
        {
            RefreshGate.Release();
        }
    }

    private static void Update(string widgetId, WidgetSnapshot snapshot)
    {
        WidgetManager.GetDefault().UpdateWidget(new WidgetUpdateRequestOptions(widgetId)
        {
            Template = WidgetCard.Render(snapshot),
            Data = "{}"
        });
    }

    private static ServiceProvider BuildServices()
    {
        var registrations = new ServiceCollection();
        registrations.AddLogging();
        registrations.AddApplication();
        registrations.AddInfrastructure(new WeatherApiOptions
        {
            BaseUrl = "https://api.weatherapi.com/v1/",
            ApiKey = WeatherApiKeyResolver.Resolve(Environment.GetEnvironmentVariable("WEATHER_API_KEY"),
                null, EmbeddedWeatherApiKey.Read),
            DefaultLocation = "auto:ip",
            ForecastDays = 3
        });
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
        return registrations.BuildServiceProvider();
    }
}
