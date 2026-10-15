using Application.Dtos;
using Application.Extensions;
using Application.Interfaces;
using Application.Queries;
using Infrastructure.Configuration;
using Infrastructure.Extensions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddLogging();
services.AddApplication();
services.AddInfrastructure(new WeatherApiOptions
{
    BaseUrl = "https://api.weatherapi.com/v1/",
    ForecastDays = 3
});
services.AddSingleton<IWeatherApiClient, StubWeatherApi>();
services.AddSingleton<IIpLocationClient, StubLocation>();
services.AddSingleton<IPlaceNameLocalizer, StubPlaceNames>();

using var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();
var response = await mediator.Send(new GetForecastQuery("ru", "58,56.25"));
if (response.Current.Condition.Text != "Пасмурно")
    throw new InvalidOperationException("The forecast handler was not resolved.");
var current = await mediator.Send(new GetCurrentWeatherQuery("58,56.25"));
if (current.Current.TempC != 8.9)
    throw new InvalidOperationException("The current weather handler was not resolved.");

var weather = await provider.GetRequiredService<IWeatherRepository>().GetWeatherAsync();
if (weather.Name != "Пермь" || weather.Current?.ConditionText != "Пасмурно" ||
    weather.Current.TempC != 8.9 || weather.HourlyForecast.Count != 1 ||
    weather.HourlyForecast[0].TempC != 9.1 || weather.DailyForecast.Count != 1 ||
    weather.DailyForecast[0].MaxtempC != 12)
    throw new InvalidOperationException("Mapping the trimmed weather data failed.");

Console.WriteLine("Trimmed weather services: OK");

internal sealed class StubWeatherApi : IWeatherApiClient
{
    private static readonly LocationDto Location = new()
    {
        Name = "Perm", Country = "Russia", Lat = 58, Lon = 56.25,
        LocalTime = "2030-04-20 15:30"
    };

    public Task<CurrentResponseDto> GetCurrentWeatherAsync(string? location = null) =>
        Task.FromResult(new CurrentResponseDto
        {
            Location = Location,
            Current = new CurrentDataDto
            {
                TempC = 8.9,
                Condition = new ConditionDto { Text = "Overcast", Code = 1009 }
            }
        });

    public Task<ForecastResponseDto> GetForecastAsync(string language = "en", string? location = null) =>
        Task.FromResult(new ForecastResponseDto
        {
            Location = Location,
            Current = new CurrentDataDto
            {
                TempC = 8.9,
                Condition = new ConditionDto { Text = language == "ru" ? "Пасмурно" : "Overcast", Code = 1009 }
            },
            Forecast = new ForecastDto
            {
                ForecastDay =
                [
                    new ForecastDayDto
                    {
                        Date = "2030-04-20",
                        Day = new DayDto { MaxtempC = 12, Condition = new ConditionDto { Code = 1009 } },
                        Hour =
                        [
                            new HourDto
                            {
                                Time = "2030-04-20 16:00", TempC = 9.1,
                                Condition = new ConditionDto { Code = 1009 }
                            }
                        ]
                    }
                ]
            }
        });
}

internal sealed class StubLocation : IIpLocationClient
{
    public Task<IpLocation> LocateAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new IpLocation("Perm", "Perm Krai", "Russia", 58, 56.25));
}

internal sealed class StubPlaceNames : IPlaceNameLocalizer
{
    public Task<LocalizedPlace?> ResolveRussianAsync(string city, string country, double latitude,
        double longitude, CancellationToken cancellationToken = default) =>
        Task.FromResult<LocalizedPlace?>(new LocalizedPlace("Пермь", "Пермский край"));
}
