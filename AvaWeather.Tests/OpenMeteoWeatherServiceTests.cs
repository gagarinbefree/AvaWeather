using System.Net;
using AvaWeather.Services;

namespace AvaWeather.Tests;

public class OpenMeteoWeatherServiceTests
{
    [Fact]
    public async Task Russian_location_maps_current_hourly_and_three_day_forecast()
    {
        var handler = new StubHandler(request =>
        {
            var query = request.RequestUri!.Query;
            Assert.Contains("latitude=55.75", query);
            Assert.Contains("longitude=37.62", query);
            Assert.Contains("timezone=auto", query);
            Assert.Contains("forecast_days=3", query);
            Assert.Contains("precipitation_probability", query);
            Assert.Contains("uv_index", query);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Response) };
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.open-meteo.com/") };
        var service = new OpenMeteoWeatherService(http,
            new StubLocation(new IpLocation("Moscow", "Moscow", "Russia", 55.75, 37.62)),
            new StubPlaceNames(new LocalizedPlace("Москва", "Москва")));

        var weather = await service.GetWeatherAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Москва", weather.Name);
        Assert.Equal("Russia", weather.Country);
        Assert.Equal(new DateTime(2026, 10, 9, 15, 30, 0), weather.LocalTime);
        Assert.Equal("Небольшой дождь", weather.Current!.ConditionText);
        Assert.Equal(12.5, weather.Current.TempC);
        Assert.Equal(9.8, weather.Current.FeelslikeC);
        Assert.Equal(1014.2, weather.Current.PressureMb);
        Assert.Equal(8, weather.Current.VisKm);
        Assert.Equal(10, weather.Current.ChanceOfRain);
        Assert.Equal(3, weather.HourlyForecast.Count);
        Assert.Equal(new DateTime(2026, 10, 9, 16, 0, 0), weather.HourlyForecast[0].Time);
        Assert.Equal("Гроза", weather.HourlyForecast[1].ConditionText);
        Assert.Equal(3, weather.DailyForecast.Count);
        Assert.Equal("Ясно", weather.DailyForecast[0].ConditionText);
        Assert.Equal("06:40", weather.DailyForecast[0].Sunrise);
        Assert.Equal(35, weather.DailyForecast[0].DailyChanceOfRain);
    }

    [Fact]
    public async Task English_location_uses_english_weather_descriptions()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent(Response) });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.open-meteo.com/") };
        var service = new OpenMeteoWeatherService(http,
            new StubLocation(new IpLocation("Berlin", "Berlin", "Germany", 52.52, 13.4)),
            new StubPlaceNames(null));

        var weather = await service.GetWeatherAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Berlin", weather.Name);
        Assert.Equal("Slight rain", weather.Current!.ConditionText);
        Assert.Equal("Thunderstorm", weather.HourlyForecast[1].ConditionText);
    }

    [Fact]
    public async Task Incomplete_forecast_fails_instead_of_showing_misleading_zeroes()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent("""{"current":{"time":"2026-10-09T15:30","temperature_2m":12}}""") });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.open-meteo.com/") };
        var service = new OpenMeteoWeatherService(http,
            new StubLocation(new IpLocation("Perm", "Perm Krai", "Russia", 58, 56)),
            new StubPlaceNames(null));

        await Assert.ThrowsAsync<InvalidDataException>(() => service.GetWeatherAsync(TestContext.Current.CancellationToken));
    }

    private sealed class StubLocation(IpLocation location) : IIpLocationClient
    {
        public Task<IpLocation> LocateAsync(CancellationToken cancellationToken = default) => Task.FromResult(location);
    }

    private sealed class StubPlaceNames(LocalizedPlace? place) : IPlaceNameLocalizer
    {
        public Task<LocalizedPlace?> ResolveRussianAsync(string city, string country, double latitude,
            double longitude, CancellationToken cancellationToken = default) => Task.FromResult(place);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private const string Response = """
    {
      "timezone":"Europe/Moscow",
      "current":{"time":"2026-10-09T15:30","temperature_2m":12.5,"relative_humidity_2m":71,"apparent_temperature":9.8,"is_day":1,"precipitation":0.2,"weather_code":61,"cloud_cover":78,"pressure_msl":1014.2,"wind_speed_10m":14,"wind_direction_10m":45,"visibility":8000,"uv_index":2.4},
      "hourly":{"time":["2026-10-09T15:00","2026-10-09T16:00","2026-10-09T17:00","2026-10-10T00:00"],"temperature_2m":[12,13,11,6],"relative_humidity_2m":[70,72,75,85],"apparent_temperature":[10,11,9,4],"precipitation_probability":[10,20,80,30],"precipitation":[0,0.1,2,0.2],"weather_code":[61,2,95,3],"wind_speed_10m":[10,12,15,8],"wind_direction_10m":[45,90,180,270],"uv_index":[2,3,1,0],"is_day":[1,1,1,0]},
      "daily":{"time":["2026-10-09","2026-10-10","2026-10-11"],"weather_code":[0,3,71],"temperature_2m_max":[15,10,5],"temperature_2m_min":[6,4,0],"temperature_2m_mean":[10,7,2],"precipitation_sum":[0,1,3],"precipitation_probability_max":[35,60,70],"wind_speed_10m_max":[20,18,15],"relative_humidity_2m_mean":[65,72,80],"uv_index_max":[4,3,2],"sunrise":["2026-10-09T06:40","2026-10-10T06:42","2026-10-11T06:44"],"sunset":["2026-10-09T17:30","2026-10-10T17:28","2026-10-11T17:26"]}
    }
    """;
}
