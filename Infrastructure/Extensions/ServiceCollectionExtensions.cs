using Application.Interfaces;
using Infrastructure.Clients;
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
        });

        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        return services;
    }
}