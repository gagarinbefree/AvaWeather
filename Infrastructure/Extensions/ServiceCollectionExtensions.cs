using Application.Interfaces;
using Infrastructure.Clients;
using Application.Services;
using Infrastructure.Configuration;
using Infrastructure.Mappers;
using Infrastructure.Diagnostics;
using Application.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherFailureReporting(this IServiceCollection services,
        string? dsn)
    {
        services.AddHttpClient("WeatherFailureReports");
        services.AddSingleton<IWeatherFailureReporter>(provider => new SentryWeatherFailureReporter(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("WeatherFailureReports"),
            dsn, provider.GetRequiredService<WeatherDiagnosticLog>()));
        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        WeatherApiOptions options)
    {
        services.AddSingleton(options);

        services.AddHttpClient<IWeatherApiClient, WeatherApiClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });

        if (options.UseDirectConnectionFallback)
        {
            services.AddHttpClient("WeatherApiDirect", client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
            }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                UseProxy = false,
                ConnectTimeout = TimeSpan.FromSeconds(10)
            });
        }

        services.AddHttpClient<GeoJsLocationClient>(client =>
        {
            client.BaseAddress = new Uri("https://get.geojs.io/");
            client.Timeout = TimeSpan.FromSeconds(4);
        });
        services.AddTransient<IIpLocationClient>(provider => provider.GetRequiredService<GeoJsLocationClient>());

        services.AddHttpClient<OpenMeteoWeatherService>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });

        services.AddHttpClient<OpenWeatherMapService>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });
        services.AddTransient<IFallbackWeatherService>(provider =>
            new FallbackWeatherService([provider.GetRequiredService<OpenMeteoWeatherService>(), provider.GetRequiredService<OpenWeatherMapService>()]));

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
