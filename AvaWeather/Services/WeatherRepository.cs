using Application.Interfaces;
using Application.Localization;
using Application.Queries;
using Application.Services;
using Domain.Entities;
using MediatR;
using System.Text.Json;

namespace AvaWeather.Services;

public sealed class WeatherRepository(
    IMediator mediator, IWeatherDataService mapper, IPlaceNameLocalizer placeNames,
    IOpenMeteoWeatherService openMeteo) : IWeatherRepository
{
    public async Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await openMeteo.GetWeatherAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return await GetWeatherApiWeatherAsync(cancellationToken);
        }
        catch (JsonException)
        {
            return await GetWeatherApiWeatherAsync(cancellationToken);
        }
        catch (InvalidDataException)
        {
            return await GetWeatherApiWeatherAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await GetWeatherApiWeatherAsync(cancellationToken);
        }
    }

    private async Task<WeatherData> GetWeatherApiWeatherAsync(CancellationToken cancellationToken)
    {
        var current = await mediator.Send(new GetCurrentWeatherQuery(), cancellationToken);
        var culture = WeatherLanguage.ForCountry(current.Location.Country);
        var forecastTask = mediator.Send(new GetForecastQuery(WeatherLanguage.ApiLanguage(culture)), cancellationToken);
        var placeTask = WeatherLanguage.ApiLanguage(culture) == "ru"
            ? placeNames.ResolveRussianAsync(current.Location.Name, current.Location.Country,
                current.Location.Lat, current.Location.Lon, cancellationToken)
            : Task.FromResult<LocalizedPlace?>(null);
        var forecast = await forecastTask;
        var place = await placeTask;
        var weather = mapper.MapToWeatherData(current, forecast);
        if (place is not null)
        {
            weather.Name = place.City;
            if (!string.IsNullOrWhiteSpace(place.Region)) weather.Region = place.Region;
            else if (weather.Region.Equals(current.Location.Name, StringComparison.OrdinalIgnoreCase))
                weather.Region = place.City;
        }
        return weather;
    }
}
