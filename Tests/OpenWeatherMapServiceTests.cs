using System.Net;
using System.Text.Json;
using Application.Interfaces;
using Infrastructure.Clients;
using Infrastructure.Configuration;

namespace Tests;

public class OpenWeatherMapServiceTests
{
    [Fact]
    public async Task Device_coordinates_resolve_country_and_request_russian_forecast()
    {
        var handler = new Handler(new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.Zero));
        var names = new RussianNames();
        var service = new OpenWeatherMapService(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") },
            new WeatherApiOptions { OpenWeatherMapApiKey = "test-key" }, placeNames: names);
        var result = await service.GetWeatherAsync(new IpLocation("", "", "", 58, 56), TestContext.Current.CancellationToken);
        Assert.Equal("ru-RU", Application.Localization.WeatherLanguage.ForCountry(result.Country).Name);
        Assert.Equal("ru", Application.Presentation.WeatherDashboard.From(result).Culture.TwoLetterISOLanguageName);
        Assert.Equal("Пермь", result.Name);
        Assert.Equal("Пермский край", result.Region);
        Assert.Equal("ясно", result.Current!.ConditionText);
        Assert.All(result.HourlyForecast, hour => Assert.Equal("ясно", hour.ConditionText));
        Assert.All(result.DailyForecast, day => Assert.Equal("ясно", day.ConditionText));
        Assert.Contains(handler.Requests, uri => uri.Contains("weather?") && uri.Contains("lang=en"));
        Assert.Contains(handler.Requests, uri => uri.Contains("weather?") && uri.Contains("lang=ru"));
        Assert.Contains(handler.Requests, uri => uri.Contains("forecast?") && uri.Contains("lang=ru"));
        Assert.Equal("Perm", names.City);
    }

    [Fact]
    public async Task Device_coordinates_in_germany_keep_english()
    {
        var handler = new Handler(new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.Zero), "DE");
        var names = new RussianNames();
        var service = new OpenWeatherMapService(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") },
            new WeatherApiOptions { OpenWeatherMapApiKey = "test-key" }, placeNames: names);
        var result = await service.GetWeatherAsync(new IpLocation("", "", "", 52, 13), TestContext.Current.CancellationToken);
        Assert.Equal("en-US", Application.Localization.WeatherLanguage.ForCountry(result.Country).Name);
        Assert.Equal("en", Application.Presentation.WeatherDashboard.From(result).Culture.TwoLetterISOLanguageName);
        Assert.Equal("clear sky", result.Current!.ConditionText);
        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, uri => Assert.Contains("lang=en", uri));
        Assert.Null(names.City);
    }

    [Fact]
    public async Task Existing_country_and_city_are_preserved_when_localizer_has_no_result()
    {
        var handler = new Handler(new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.Zero));
        var service = new OpenWeatherMapService(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") },
            new WeatherApiOptions { OpenWeatherMapApiKey = "test-key" });
        var result = await service.GetWeatherAsync(new IpLocation("Perm", "Perm", "Russia", 58, 56), TestContext.Current.CancellationToken);
        Assert.Equal("Perm", result.Name);
        Assert.Equal("Russia", result.Country);
        Assert.Equal("ясно", result.Current!.ConditionText);
    }

    [Fact]
    public async Task Maps_three_hour_forecast_and_leaves_unavailable_uv_empty()
    {
        var now = new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);
        var handler = new Handler(now);
        var service = new OpenWeatherMapService(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") },
            new WeatherApiOptions { OpenWeatherMapApiKey = "test-key" });
        var result = await service.GetWeatherAsync(new IpLocation("Perm", "Perm", "Russia", 58, 56), TestContext.Current.CancellationToken);
        Assert.Equal(3, result.DailyForecast.Count);
        Assert.Null(result.Current!.Uv);
        Assert.Null(result.DailyForecast[0].Uv);
        Assert.Equal(1000, result.Current.PressureMb);
        Assert.Equal(10.8, result.Current.WindKph, 2);
        Assert.Equal("—", result.DailyForecast[1].Sunrise);
        Assert.Equal(TimeSpan.FromHours(3), result.HourlyForecast[1].Time - result.HourlyForecast[0].Time);
        Assert.All(handler.Requests, uri => Assert.Contains("lang=ru", uri));
    }

    private sealed class Handler(DateTimeOffset now, string country = "RU") : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!.AbsoluteUri);
            var common = new { main = new { temp = 10, feels_like = 9, humidity = 65, pressure = 1000 },
                weather = new[] { new { id = 800, description = request.RequestUri.Query.Contains("lang=ru") ? "ясно" : "clear sky", icon = "01d" } }, wind = new { speed = 3, deg = 180 } };
            object body = request.RequestUri.AbsolutePath.EndsWith("weather")
                ? new { dt = now.ToUnixTimeSeconds(), timezone = 0, main = common.main, weather = common.weather,
                    wind = common.wind, visibility = 10000, sys = new { country, sunrise = now.AddHours(-3).ToUnixTimeSeconds(), sunset = now.AddHours(9).ToUnixTimeSeconds() }, name = "Perm" }
                : new { list = Enumerable.Range(1, 32).Select(i => new { dt = now.AddHours(i * 3).ToUnixTimeSeconds(),
                    main = common.main, weather = common.weather, wind = common.wind, pop = 0.4 }).ToArray() };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body)) });
        }
    }

    private sealed class RussianNames : IPlaceNameLocalizer
    {
        public string? City { get; private set; }
        public Task<LocalizedPlace?> ResolveRussianAsync(string city, string country, double latitude, double longitude,
            CancellationToken cancellationToken = default)
        {
            City = city;
            return Task.FromResult<LocalizedPlace?>(new LocalizedPlace("Пермь", "Пермский край"));
        }
    }
}
