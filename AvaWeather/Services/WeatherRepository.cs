using Application.Interfaces;
using Application.Localization;
using Application.Queries;
using Application.Services;
using Domain.Entities;
using MediatR;

namespace AvaWeather.Services;

public sealed class WeatherRepository(IMediator mediator, IWeatherDataService mapper) : IWeatherRepository
{
    public async Task<WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default)
    {
        var current = await mediator.Send(new GetCurrentWeatherQuery(), cancellationToken);
        var culture = WeatherLanguage.ForCountry(current.Location.Country);
        var forecast = await mediator.Send(new GetForecastQuery(WeatherLanguage.ApiLanguage(culture)), cancellationToken);
        return mapper.MapToWeatherData(current, forecast);
    }
}
