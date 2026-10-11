using Application.Extensions;
using Application.Services;
using Application.Interfaces;
using Infrastructure.Clients;
using Infrastructure.Configuration;
using Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AvaWeather.Tests;

public class SharedWeatherServicesTests
{
    [Fact]
    public void Weather_workflow_is_available_without_the_avalonia_assembly()
    {
        Assert.Equal(typeof(WeatherDataService).Assembly, typeof(WeatherRepository).Assembly);
        Assert.Equal(typeof(WeatherDataService).Assembly, typeof(IWeatherRepository).Assembly);
        Assert.Equal(typeof(WeatherDataService).Assembly, typeof(IIpLocationClient).Assembly);
        Assert.Equal(typeof(WeatherDataService).Assembly, typeof(IPlaceNameLocalizer).Assembly);
        Assert.Equal(typeof(WeatherApiClient).Assembly, typeof(GeoJsLocationClient).Assembly);
        Assert.Equal(typeof(WeatherApiClient).Assembly, typeof(OpenMeteoPlaceNameLocalizer).Assembly);
    }

    [Fact]
    public void Browser_registration_resolves_shared_weather_services_without_a_direct_socket_client()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(new WeatherApiOptions
        {
            BaseUrl = "https://api.weatherapi.com/v1/",
            ApiKey = "test",
            ForecastDays = 3
        });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<WeatherRepository>(scope.ServiceProvider.GetRequiredService<IWeatherRepository>());
        Assert.IsType<GeoJsLocationClient>(scope.ServiceProvider.GetRequiredService<IIpLocationClient>());
        Assert.IsType<OpenMeteoPlaceNameLocalizer>(scope.ServiceProvider.GetRequiredService<IPlaceNameLocalizer>());
        Assert.IsType<WeatherApiClient>(scope.ServiceProvider.GetRequiredService<IWeatherApiClient>());
        using var direct = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>()
            .CreateClient("WeatherApiDirect");
        Assert.Null(direct.BaseAddress);
    }
}
