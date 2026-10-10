using System.Net;
using Application.Interfaces;
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

    [Fact]
    public async Task Diagnostic_events_report_status_without_exposing_the_api_key()
    {
        var log = new RecordingApiLog();
        using var http = new HttpClient(new ErrorHandler()) { BaseAddress = new Uri("https://api.weatherapi.com/v1/") };
        var client = new WeatherApiClient(http, new WeatherApiOptions { ApiKey = "secret-for-test" }, log);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetForecastAsync("ru", "58.0047,56.2514"));

        Assert.Contains(log.Events, entry => entry.Service == "WeatherAPI" &&
            entry.Operation == "forecast" && entry.Outcome == "HttpError" &&
            entry.StatusCode == 401 && entry.LocationMode == "coordinates");
        Assert.DoesNotContain("secret-for-test", string.Join("\n", log.Events));
    }

    [Fact]
    public async Task Transport_error_log_does_not_copy_exception_message_or_url()
    {
        var log = new RecordingApiLog();
        using var http = new HttpClient(new TransportFailureHandler())
        {
            BaseAddress = new Uri("https://api.weatherapi.com/v1/")
        };
        var client = new WeatherApiClient(http, new WeatherApiOptions { ApiKey = "secret-for-test" }, log);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetCurrentWeatherAsync());

        Assert.Contains(log.Events, entry => entry.Outcome == "TransportError");
        Assert.DoesNotContain("secret-for-test", string.Join("\n", log.Events));
        Assert.DoesNotContain("current.json", string.Join("\n", log.Events));
    }

    [Fact]
    public async Task Transport_failure_retries_direct_connection_and_records_both_routes()
    {
        var log = new RecordingApiLog();
        using var primary = new HttpClient(new TransportFailureHandler())
        {
            BaseAddress = new Uri("https://api.weatherapi.com/v1/")
        };
        var directHandler = new RecordingHandler();
        using var direct = new HttpClient(directHandler)
        {
            BaseAddress = primary.BaseAddress
        };
        var client = new WeatherApiClient(primary,
            new WeatherApiOptions { ApiKey = "secret-for-test" }, log,
            new DirectClientFactory(direct));

        await client.GetCurrentWeatherAsync("58.0047,56.2514");

        Assert.Single(directHandler.Requests);
        Assert.Contains(log.Events, entry => entry.Outcome == "TransportError" &&
            entry.ConnectionRoute == "system");
        Assert.Contains(log.Events, entry => entry.Outcome == "Success" &&
            entry.ConnectionRoute == "direct");
        Assert.DoesNotContain("secret-for-test", string.Join("\n", log.Events));
    }

    [Fact]
    public async Task Http_error_does_not_retry_direct_connection()
    {
        var log = new RecordingApiLog();
        using var primary = new HttpClient(new ErrorHandler())
        {
            BaseAddress = new Uri("https://api.weatherapi.com/v1/")
        };
        var directHandler = new RecordingHandler();
        using var direct = new HttpClient(directHandler)
        {
            BaseAddress = primary.BaseAddress
        };
        var client = new WeatherApiClient(primary,
            new WeatherApiOptions { ApiKey = "secret-for-test" }, log,
            new DirectClientFactory(direct));

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetCurrentWeatherAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        Assert.Empty(directHandler.Requests);
        Assert.Contains(log.Events, entry => entry.Outcome == "HttpError" &&
            entry.ConnectionRoute == "system");
    }

    private sealed class RecordingApiLog : IApiAccessLog
    {
        public List<ApiAccessEvent> Events { get; } = [];
        public void Write(ApiAccessEvent entry) => Events.Add(entry);
    }

    private sealed class DirectClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal("WeatherApiDirect", name);
            return client;
        }
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

    private sealed class TransportFailureHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException(
                "https://api.weatherapi.com/v1/current.json?key=secret-for-test failed"));
    }
}
