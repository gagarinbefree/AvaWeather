using System.Net;
using System.Net.Http;
using Application.Dtos;
using Application.Extensions;
using Application.Interfaces;
using Application.Services;
using AvaWeather.Services;
using AvaWeather.ViewModels;
using Infrastructure.Mappers;
using Microsoft.Extensions.DependencyInjection;

namespace AvaWeather.Tests;

public class PlaceNameLocalizationTests
{
    [Fact]
    public async Task Russian_name_is_selected_for_the_matching_weather_location()
    {
        var handler = new StubHandler(request =>
        {
            Assert.Contains("name=Almaty%2CKazakhstan", request.RequestUri!.Query);
            Assert.Contains("language=ru", request.RequestUri.Query);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {"results":[
                  {"name":"Алмати","admin1":"Другая область","latitude":46.7324,"longitude":-117.0002},
                  {"name":"Алматы","admin1":"Алматы","latitude":43.2389,"longitude":76.8897}
                ]}
                """)
            };
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://geocoding-api.open-meteo.com/") };
        var resolver = new OpenMeteoPlaceNameLocalizer(http);

        var place = await resolver.ResolveRussianAsync("Almaty", "Kazakhstan", 43.24, 76.89,
            TestContext.Current.CancellationToken);

        Assert.Equal("Алматы", place?.City);
        Assert.Equal("Алматы", place?.Region);
    }

    [Fact]
    public async Task Unrelated_search_result_does_not_replace_weather_location()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {"results":[{"name":"Павловск","latitude":50.0,"longitude":30.0}]}
            """)
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://geocoding-api.open-meteo.com/") };

        var place = await new OpenMeteoPlaceNameLocalizer(http)
            .ResolveRussianAsync("Pavlovsk", "Russia", 59.68, 30.45, TestContext.Current.CancellationToken);

        Assert.Null(place);
    }

    [Theory]
    [InlineData("Moscow", 1)]
    [InlineData("Moskow", 2)]
    [InlineData("Moskva", 1)]
    public async Task Moscow_aliases_are_resolved_by_geocoding_and_coordinates(string city, int expectedRequests)
    {
        var queries = new List<string>();
        var handler = new StubHandler(request =>
        {
            var query = request.RequestUri!.Query;
            queries.Add(query);
            Assert.Contains("language=ru", query);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(query.Contains("name=Moskow%2CRussia", StringComparison.Ordinal)
                    ? """{"results":[{"name":"Московское","latitude":51.41111,"longitude":39.60278}]}"""
                    : """{"results":[{"name":"Москва","admin1":"Москва","latitude":55.75204,"longitude":37.61781}]}""")
            };
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://geocoding-api.open-meteo.com/") };

        var place = await new OpenMeteoPlaceNameLocalizer(http)
            .ResolveRussianAsync(city, "Russia", 55.75, 37.62, TestContext.Current.CancellationToken);

        Assert.Equal("Москва", place?.City);
        Assert.Equal(expectedRequests, queries.Count);
        Assert.Contains($"name={city}%2CRussia", queries[0]);
        if (city == "Moskow") Assert.Contains("name=Mosk%2CRussia", queries[1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Repository_displays_localized_city_when_open_meteo_is_unavailable(bool timeout)
    {
        var weatherApi = new StubWeatherApi();
        var placeNames = new StubPlaceNames();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
        services.AddSingleton<IWeatherApiClient>(weatherApi);
        services.AddSingleton<IIpLocationClient>(new StubIpLocation());
        services.AddSingleton<IPlaceNameLocalizer>(placeNames);
        services.AddSingleton<IOpenMeteoWeatherService>(new UnavailableOpenMeteo(timeout));
        services.AddTransient<IWeatherRepository, WeatherRepository>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var viewModel = new WeatherViewModel(scope.ServiceProvider.GetRequiredService<IWeatherRepository>());

        await viewModel.LoadAsync();

        Assert.Null(viewModel.ErrorMessage);
        Assert.Equal("55.75,37.62", weatherApi.CurrentLocation);
        Assert.Equal("ru", weatherApi.ForecastLanguage);
        Assert.Equal("55.7558,37.6173", weatherApi.ForecastLocation);
        Assert.Equal("Москва", viewModel.HeaderLocation);
        Assert.Equal("Москва, Россия", viewModel.Display!.Location);
        Assert.Equal("Москва", viewModel.Display.Region);
        Assert.Equal("Солнечно", viewModel.Display.Condition);
        Assert.Equal(1, placeNames.Calls);
    }

    private sealed class StubWeatherApi : IWeatherApiClient
    {
        public string? CurrentLocation { get; private set; }
        public string? ForecastLanguage { get; private set; }
        public string? ForecastLocation { get; private set; }

        public Task<CurrentResponseDto> GetCurrentWeatherAsync(string? location = null)
        {
            CurrentLocation = location;
            return Task.FromResult(new CurrentResponseDto
            {
                Location = new LocationDto
                {
                    Name = "Moscow", Region = "Moscow", Country = "Russia",
                    Lat = 55.7558, Lon = 37.6173, LocalTime = "2026-10-09 15:00"
                },
                Current = new CurrentDataDto { Condition = new ConditionDto { Text = "Sunny", Code = 1000 } }
            });
        }

        public Task<ForecastResponseDto> GetForecastAsync(string language = "en", string? location = null)
        {
            ForecastLocation = location;
            ForecastLanguage = language;
            return Task.FromResult(new ForecastResponseDto
            {
                Current = new CurrentDataDto { Condition = new ConditionDto { Text = "Солнечно", Code = 1000 } }
            });
        }
    }

    private sealed class StubIpLocation : IIpLocationClient
    {
        public Task<IpLocation> LocateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new IpLocation("Moscow", "Moscow", "Russia", 55.75, 37.62));
    }

    private sealed class StubPlaceNames : IPlaceNameLocalizer
    {
        public int Calls { get; private set; }

        public Task<LocalizedPlace?> ResolveRussianAsync(string city, string country, double latitude,
            double longitude, CancellationToken cancellationToken = default)
        {
            Calls++;
            Assert.Equal("Moscow", city);
            Assert.Equal("Russia", country);
            Assert.Equal(55.7558, latitude);
            return Task.FromResult<LocalizedPlace?>(new LocalizedPlace("Москва", "Москва"));
        }
    }

    private sealed class UnavailableOpenMeteo(bool timeout) : IOpenMeteoWeatherService
    {
        public Task<Domain.Entities.WeatherData> GetWeatherAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<Domain.Entities.WeatherData>(timeout
                ? new TaskCanceledException("Open-Meteo timed out")
                : new HttpRequestException("Open-Meteo unavailable"));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
