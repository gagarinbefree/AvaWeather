using System.Text.Json;
using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Configuration;

namespace Infrastructure.Clients;

public class WeatherApiClient : IWeatherApiClient
{
    private readonly HttpClient _httpClient;
    private readonly WeatherApiOptions _options;

    public WeatherApiClient(HttpClient httpClient, WeatherApiOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<CurrentResponseDto> GetCurrentWeatherAsync()
    {
        var url = $"current.json?key={_options.ApiKey}&q={_options.DefaultLocation}";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Failed to get current weather: {response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<CurrentResponseDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize current weather response");
    }

    public async Task<ForecastResponseDto> GetForecastAsync()
    {
        var url = $"forecast.json?key={_options.ApiKey}&q={_options.DefaultLocation}&days={_options.ForecastDays}";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Failed to get forecast: {response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<ForecastResponseDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize forecast response");
    }
}