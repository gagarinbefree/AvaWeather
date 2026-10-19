using Application.Interfaces;
using Application.Localization;
using Application.Queries;
using Application.Dtos;
using Domain.Entities;
using MediatR;
using System.Text.Json;

namespace Application.Services;

public sealed class WeatherRepository(
    IMediator mediator, IWeatherDataService mapper, IPlaceNameLocalizer placeNames,
    IIpLocationClient locations, IFallbackWeatherService? fallback = null) : IWeatherRepository
{
    public async Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default)
    {
        var detectedLocation = await DetectLocationAsync(cancellationToken);
        try
        {
            return await GetPrimaryWeatherAsync(detectedLocation, cancellationToken);
        }
        catch (Exception error) when (fallback is not null && detectedLocation is not null &&
            Coordinates(detectedLocation.Latitude, detectedLocation.Longitude) is not null &&
            !cancellationToken.IsCancellationRequested &&
            error is HttpRequestException or OperationCanceledException or JsonException or InvalidDataException)
        {
            return await fallback.GetWeatherAsync(detectedLocation, cancellationToken);
        }
    }

    private async Task<WeatherData> GetPrimaryWeatherAsync(IpLocation? detectedLocation,
        CancellationToken cancellationToken)
    {
        var coordinates = detectedLocation is null
            ? null : Coordinates(detectedLocation.Latitude, detectedLocation.Longitude);
        var language = WeatherLanguage.ApiLanguage(WeatherLanguage.ForCountry(detectedLocation?.Country));
        var forecast = await mediator.Send(new GetForecastQuery(language, coordinates), cancellationToken);
        // When GeoJS is unavailable, WeatherAPI's response is the first reliable source
        // for the country. Fetch localized condition text only if that changes the language.
        var culture = WeatherLanguage.ForCountry(forecast.Location.Country);
        var resolvedLanguage = WeatherLanguage.ApiLanguage(culture);
        if (resolvedLanguage != language)
        {
            forecast = await mediator.Send(new GetForecastQuery(resolvedLanguage, coordinates), cancellationToken);
        }

        var placeTask = WeatherLanguage.ApiLanguage(culture) == "ru"
            ? placeNames.ResolveRussianAsync(forecast.Location.Name, forecast.Location.Country,
                forecast.Location.Lat, forecast.Location.Lon, cancellationToken)
            : Task.FromResult<LocalizedPlace?>(null);
        var place = await placeTask;
        var current = new CurrentResponseDto { Location = forecast.Location, Current = forecast.Current };
        var weather = mapper.MapToWeatherData(current, forecast);
        if (place is not null)
        {
            weather.Name = place.City;
            if (!string.IsNullOrWhiteSpace(place.Region)) weather.Region = place.Region;
            else if (weather.Region.Equals(forecast.Location.Name, StringComparison.OrdinalIgnoreCase))
                weather.Region = place.City;
        }
        return weather;
    }

    private async Task<IpLocation?> DetectLocationAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await locations.LocateAsync(cancellationToken);
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
