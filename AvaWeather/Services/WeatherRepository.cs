using Application.Interfaces;
using Application.Localization;
using Application.Queries;
using Application.Services;
using Domain.Entities;
using MediatR;

namespace AvaWeather.Services;

public sealed class WeatherRepository(IMediator mediator, IWeatherDataService mapper, IPlaceNameLocalizer placeNames) : IWeatherRepository
{
    public async Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default)
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
