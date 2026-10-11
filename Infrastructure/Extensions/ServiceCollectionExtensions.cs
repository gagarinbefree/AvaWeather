using Application.Interfaces;
using Infrastructure.Clients;
using Application.Services;
using Infrastructure.Configuration;
using Infrastructure.Mappers;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        WeatherApiOptions options)
    {
        services.AddSingleton(options);

        services.AddHttpClient<IWeatherApiClient, WeatherApiClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        if (options.UseDirectConnectionFallback)
        {
            services.AddHttpClient("WeatherApiDirect", client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(20);
            }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                UseProxy = false,
                ConnectTimeout = TimeSpan.FromSeconds(10)
            });
        }

        services.AddHttpClient<IIpLocationClient, GeoJsLocationClient>(client =>
        {
            client.BaseAddress = new Uri("https://get.geojs.io/");
            client.Timeout = TimeSpan.FromSeconds(4);
        });

        services.AddHttpClient<IPlaceNameLocalizer, OpenMeteoPlaceNameLocalizer>(client =>
        {
            client.BaseAddress = new Uri("https://geocoding-api.open-meteo.com/");
            client.Timeout = TimeSpan.FromSeconds(3);
        });

        services.AddTransient<IWeatherRepository, WeatherRepository>();

        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        return services;
    }
}
