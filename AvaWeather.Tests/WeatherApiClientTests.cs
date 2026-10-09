using System.Net;
using Infrastructure.Clients;
using Infrastructure.Configuration;

namespace AvaWeather.Tests;

public class WeatherApiClientTests
{
    [Fact]
    public async Task Requests_use_detected_ip_location_by_default()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.weatherapi.com/v1/") };
        var client = new WeatherApiClient(http, new WeatherApiOptions { ApiKey = "test", ForecastDays = 3 });

        await client.GetCurrentWeatherAsync();
        await client.GetForecastAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.Contains("q=auto%3Aip", request.Query));
    }

    [Fact]
    public async Task Requests_use_location_from_configuration()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.weatherapi.com/v1/") };
        var client = new WeatherApiClient(http, new WeatherApiOptions
        {
            ApiKey = "test", DefaultLocation = "New York", ForecastDays = 3
        });

        await client.GetCurrentWeatherAsync();
        await client.GetForecastAsync();

        Assert.All(handler.Requests, request => Assert.Contains("q=New%20York", request.Query));
    }

    [Fact]
    public async Task Russian_forecast_requests_localized_conditions()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.weatherapi.com/v1/") };
        var client = new WeatherApiClient(http, new WeatherApiOptions { ApiKey = "test" });

        await client.GetForecastAsync("ru");

        Assert.Contains("lang=ru", Assert.Single(handler.Requests).Query);
    }

    [Fact]
    public async Task Forecast_uses_coordinates_from_current_response_instead_of_second_ip_lookup()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.weatherapi.com/v1/") };
        var client = new WeatherApiClient(http, new WeatherApiOptions { ApiKey = "test" });

        await client.GetForecastAsync("ru", "58.0047,56.2514");

        var request = Assert.Single(handler.Requests);
        Assert.Contains("q=58.0047%2C56.2514", request.Query);
        Assert.DoesNotContain("auto%3Aip", request.Query);
    }

    [Fact]
    public async Task Current_weather_can_use_geojs_coordinates_when_ip_detection_varies_by_request()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.weatherapi.com/v1/") };
        var client = new WeatherApiClient(http, new WeatherApiOptions { ApiKey = "test" });

        await client.GetCurrentWeatherAsync("58.0047,56.2514");

        Assert.Contains("q=58.0047%2C56.2514", Assert.Single(handler.Requests).Query);
    }

    [Fact]
    public async Task Rejected_api_request_preserves_http_status_for_the_ui()
    {
        using var http = new HttpClient(new ErrorHandler()) { BaseAddress = new Uri("https://api.weatherapi.com/v1/") };
        var client = new WeatherApiClient(http, new WeatherApiOptions { ApiKey = "test" });

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetForecastAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        Assert.DoesNotContain("test", error.Message);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        }
    }

    private sealed class ErrorHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
    }
}
