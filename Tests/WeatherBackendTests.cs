using System.Net;
using Application.Dtos;
using Application.Extensions;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Infrastructure.Clients;
using Infrastructure.Configuration;
using Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public sealed class WeatherBackendTests
{
    [Fact]
    public void All_weather_clients_use_the_shared_backend_without_credentials()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(new WeatherApiOptions());
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        foreach (var name in new[] { nameof(IWeatherApiClient), nameof(OpenMeteoWeatherService), nameof(OpenWeatherMapService) })
        {
            using var client = factory.CreateClient(name);
            Assert.Equal("https://gagarinbefree.clodocd.com/", client.BaseAddress?.AbsoluteUri);
            Assert.Equal(TimeSpan.FromSeconds(40), client.Timeout);
        }
        Assert.NotNull(provider.GetRequiredService<IFallbackWeatherService>());
    }

    [Fact]
    public void Retained_server_response_keeps_its_original_timestamp()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-Weather-Cache", "stale");
        response.Headers.Add("X-Weather-Fetched-At", "2026-10-29T04:00:00.0000000Z");
        Assert.Equal(new DateTimeOffset(2026, 10, 29, 4, 0, 0, TimeSpan.Zero), WeatherBackendResponse.ReadRetainedAt(response));
    }

    [Fact]
    public void Fresh_server_cache_is_not_presented_as_a_saved_forecast()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-Weather-Cache", "fresh");
        response.Headers.Add("X-Weather-Fetched-At", "2026-10-29T04:00:00Z");
        Assert.Null(WeatherBackendResponse.ReadRetainedAt(response));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    public void Retained_response_without_a_valid_timestamp_is_rejected(string? value)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-Weather-Cache", "stale");
        if (value is not null) response.Headers.Add("X-Weather-Fetched-At", value);
        Assert.Throws<InvalidDataException>(() => WeatherBackendResponse.ReadRetainedAt(response));
    }

    [Fact]
    public async Task Fresh_backup_replaces_a_retained_response()
    {
        var stale = new WeatherData { ServerCachedAtUtc = DateTimeOffset.UtcNow.AddHours(-1) };
        var fresh = new WeatherData();
        var service = new FallbackWeatherService([new Source(stale), new Source(fresh)]);
        Assert.Same(fresh, await service.GetWeatherAsync(new IpLocation("", "", "", 58, 56), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Latest_retained_backup_survives_other_provider_failures()
    {
        var older = new WeatherData { ServerCachedAtUtc = DateTimeOffset.UtcNow.AddHours(-2) };
        var newer = new WeatherData { ServerCachedAtUtc = DateTimeOffset.UtcNow.AddHours(-1) };
        var service = new FallbackWeatherService([new Source(older), new Source(newer), new Source(null)]);
        Assert.Same(newer, await service.GetWeatherAsync(new IpLocation("", "", "", 58, 56), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Shared_rate_limits_are_distinct_from_provider_rate_limits()
    {
        using var shared = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        Assert.Throws<WeatherBackendRateLimitException>(() => WeatherBackendResponse.EnsureSuccess(shared));
        using var provider = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        provider.Headers.Add("X-Weather-Provider", "WeatherAPI");
        Assert.Throws<HttpRequestException>(() => WeatherBackendResponse.EnsureSuccess(provider));
    }

    [Fact]
    public async Task Shared_rate_limit_does_not_start_another_provider_request()
    {
        var backup = new Source(new WeatherData());
        var service = new FallbackWeatherService([new Source(null, sharedLimit: true), backup]);
        await Assert.ThrowsAsync<WeatherBackendRateLimitException>(() => service.GetWeatherAsync(
            new IpLocation("", "", "", 58, 56), TestContext.Current.CancellationToken));
        Assert.Equal(0, backup.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Primary_retained_weather_prefers_live_backups_and_survives_backup_failures(bool liveBackup)
    {
        var timestamp = new DateTimeOffset(2026, 10, 28, 4, 0, 0, TimeSpan.Zero);
        var fresh = new WeatherData { Name = "Live" };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(new WeatherApiOptions());
        services.AddSingleton<IWeatherApiClient>(new Primary(timestamp));
        services.AddSingleton<IIpLocationClient, Location>();
        services.AddSingleton<IFallbackWeatherService>(new Source(liveBackup ? fresh : null));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var weather = await scope.ServiceProvider.GetRequiredService<IWeatherRepository>().GetWeatherAsync(TestContext.Current.CancellationToken);
        if (liveBackup) Assert.Same(fresh, weather);
        else Assert.Equal(timestamp, weather.ServerCachedAtUtc);
    }

    private sealed class Primary(DateTimeOffset retainedAt) : IWeatherApiClient
    {
        /// <summary>GetCurrentWeatherAsync. Reject unused extra current requests.</summary>
        /// <param name="location">The requested weather location.</param>
        /// <returns>No result because the repository uses the forecast response.</returns>
        public Task<CurrentResponseDto> GetCurrentWeatherAsync(string? location = null) => throw new InvalidOperationException();

        /// <summary>GetForecastAsync. Provide native forecast data with retained server metadata.</summary>
        /// <param name="language">The requested condition language.</param>
        /// <param name="location">The requested coordinates.</param>
        /// <returns>The controlled retained forecast response.</returns>
        public Task<ForecastResponseDto> GetForecastAsync(string language = "en", string? location = null) => Task.FromResult(new ForecastResponseDto
        {
            ServerCachedAtUtc = retainedAt,
            Location = new LocationDto { Name = "Retained", Country = "Germany", Lat = 58, Lon = 56, LocalTime = "2026-10-28 10:00" }
        });
    }

    private sealed class Location : IIpLocationClient
    {
        /// <summary>LocateAsync. Supply a deterministic position without network requests.</summary>
        /// <param name="cancellationToken">The location cancellation token.</param>
        /// <returns>The test location used by the repository.</returns>
        public Task<IpLocation> LocateAsync(CancellationToken cancellationToken = default) => Task.FromResult(new IpLocation("Test", "", "Germany", 58, 56));
    }

    private sealed class Source(WeatherData? weather, bool sharedLimit = false) : IFallbackWeatherService
    {
        public int Calls { get; private set; }

        /// <summary>GetWeatherAsync. Return controlled live or retained weather.</summary>
        /// <param name="location">The detected device position.</param>
        /// <param name="cancellationToken">The caller cancellation token.</param>
        /// <returns>The selected response or a simulated provider failure.</returns>
        public Task<WeatherData> GetWeatherAsync(IpLocation location, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (sharedLimit) throw new WeatherBackendRateLimitException();

            return weather is null ? Task.FromException<WeatherData>(new HttpRequestException()) : Task.FromResult(weather);
        }
    }
}
