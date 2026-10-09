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
}
