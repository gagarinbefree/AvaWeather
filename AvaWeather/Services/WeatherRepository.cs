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
    IIpLocationClient locations) : IWeatherRepository
{
    public async Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default)
    {
        var detectedLocation = await DetectLocationAsync(cancellationToken);
        var current = await mediator.Send(new GetCurrentWeatherQuery(detectedLocation), cancellationToken);
        var culture = WeatherLanguage.ForCountry(current.Location.Country);
        var location = current.Location.Lat is >= -90 and <= 90 &&
                       current.Location.Lon is >= -180 and <= 180 &&
                       (current.Location.Lat != 0 || current.Location.Lon != 0)
            ? FormattableString.Invariant($"{current.Location.Lat},{current.Location.Lon}")
            : current.Location.Name;
        var forecastTask = mediator.Send(new GetForecastQuery(WeatherLanguage.ApiLanguage(culture), location), cancellationToken);
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

    private async Task<string?> DetectLocationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var location = await locations.LocateAsync(cancellationToken);
            return Coordinates(location.Latitude, location.Longitude);
        }
        catch (HttpRequestException) { return null; }
        catch (JsonException) { return null; }
        catch (InvalidDataException) { return null; }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return null; }
    }

    private static string? Coordinates(double latitude, double longitude) =>
        latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180 &&
        (latitude != 0 || longitude != 0)
            ? FormattableString.Invariant($"{latitude},{longitude}")
            : null;
}
