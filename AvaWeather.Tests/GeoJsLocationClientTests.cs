using System.Net;
using AvaWeather.Services;

namespace AvaWeather.Tests;

public class GeoJsLocationClientTests
{
    [Fact]
    public async Task Reads_city_country_and_string_coordinates()
    {
        using var http = new HttpClient(new StubHandler("""
            {"city":"Perm","region":"Perm Krai","country":"Russia","latitude":"58.0105","longitude":"56.2502"}
            """)) { BaseAddress = new Uri("https://get.geojs.io/") };

        var place = await new GeoJsLocationClient(http).LocateAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Perm", place.City);
        Assert.Equal("Perm Krai", place.Region);
        Assert.Equal("Russia", place.Country);
        Assert.Equal(58.0105, place.Latitude);
        Assert.Equal(56.2502, place.Longitude);
    }

    [Fact]
    public async Task Missing_city_is_an_error_so_repository_can_use_weather_api()
    {
        using var http = new HttpClient(new StubHandler("""
            {"country":"Russia","latitude":"58.0105","longitude":"56.2502"}
            """)) { BaseAddress = new Uri("https://get.geojs.io/") };

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new GeoJsLocationClient(http).LocateAsync(TestContext.Current.CancellationToken));
    }

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("https://get.geojs.io/v1/ip/geo.json", request.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
}
