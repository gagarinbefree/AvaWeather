using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Application.Diagnostics;
using Application.Interfaces;

namespace Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherDiagnostics(this IServiceCollection services,
        IApiAccessLog? debugSink = null)
    {
        var log = new WeatherDiagnosticLog(debugSink);
        services.AddSingleton(log);
        services.AddSingleton<IApiAccessLog>(log);
        services.AddSingleton<IWeatherFailureReporter>(DisabledWeatherFailureReporter.Instance);
        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        });

        services.AddScoped<IWeatherDataService, WeatherDataService>();
        services.AddSingleton(Application.Theming.ThemeColorService.Default);

        return services;
    }
}
