using Application.Interfaces;
using Application.Services;
using Application.Extensions;
using Application.Dtos;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Mappers;

namespace Tests;

public class WeatherSourceSelectionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_primary_uses_reserve_with_detected_coordinates(bool timeout)
    {
        var reserve = new Reserve();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
        services.AddSingleton<IWeatherApiClient>(new FailedWeatherApi(timeout));
        services.AddSingleton<IIpLocationClient>(new Location());
        services.AddSingleton<IPlaceNameLocalizer>(new NoNames());
        services.AddSingleton<IFallbackWeatherService>(reserve);
        services.AddTransient<IWeatherRepository, WeatherRepository>();
        using var provider = services.BuildServiceProvider();

        var weather = await provider.GetRequiredService<IWeatherRepository>()
            .GetWeatherAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Perm", weather.Name);
        Assert.Equal(58.0, reserve.Location!.Latitude);
    }

    [Fact]
    public async Task Failure_of_both_sources_is_reported()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
        services.AddSingleton<IWeatherApiClient>(new FailedWeatherApi(false));
        services.AddSingleton<IIpLocationClient>(new Location());
        services.AddSingleton<IPlaceNameLocalizer>(new NoNames());
        services.AddSingleton<IFallbackWeatherService>(new FailedReserve());
        services.AddTransient<IWeatherRepository, WeatherRepository>();
        using var provider = services.BuildServiceProvider();

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetRequiredService<IWeatherRepository>()
            .GetWeatherAsync(TestContext.Current.CancellationToken));
    }

    private sealed class FailedReserve : IFallbackWeatherService
    {
        public Task<WeatherData> GetWeatherAsync(IpLocation location, CancellationToken cancellationToken = default) =>
            Task.FromException<WeatherData>(new HttpRequestException("Open-Meteo unavailable"));
    }

    private sealed class FailedWeatherApi(bool timeout) : IWeatherApiClient
    {
        public Task<ForecastResponseDto> GetForecastAsync(string language = "en", string? location = null) =>
            Task.FromException<ForecastResponseDto>(timeout ? new TaskCanceledException() : new HttpRequestException());
        public Task<CurrentResponseDto> GetCurrentWeatherAsync(string? location = null) =>
            Task.FromException<CurrentResponseDto>(new HttpRequestException());
    }

    private sealed class Location : IIpLocationClient
    {
        public Task<IpLocation> LocateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new IpLocation("Perm", "Perm", "Russia", 58, 56));
    }

    private sealed class Reserve : IFallbackWeatherService
    {
        public IpLocation? Location { get; private set; }
        public Task<WeatherData> GetWeatherAsync(IpLocation location, CancellationToken cancellationToken = default)
        {
            Location = location;
            return Task.FromResult(new WeatherData { Name = location.City });
        }
    }

    private sealed class NoNames : IPlaceNameLocalizer
    {
        public Task<LocalizedPlace?> ResolveRussianAsync(string city, string country, double latitude,
            double longitude, CancellationToken cancellationToken = default) => Task.FromResult<LocalizedPlace?>(null);
    }
    [Fact]
    public async Task Caller_cancellation_is_not_ignored_during_location_detection()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var repository = new WeatherRepository(null!, null!, null!,
            new CancelingLocation());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetWeatherAsync(cancelled.Token));
    }

    private sealed class CancelingLocation : IIpLocationClient
    {
        public Task<IpLocation> LocateAsync(CancellationToken cancellationToken = default) =>
            Task.FromCanceled<IpLocation>(cancellationToken);
    }
}
