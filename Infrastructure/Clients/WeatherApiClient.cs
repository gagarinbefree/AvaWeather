using System.Text.Json;
using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Configuration;
using Weather.Localization;

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

    public async Task<CurrentResponseDto> GetCurrentWeatherAsync(string? location = null)
    {
        var queryLocation = string.IsNullOrWhiteSpace(location) ? _options.DefaultLocation : location;
        var url = $"current.json?key={Uri.EscapeDataString(_options.ApiKey)}&q={Uri.EscapeDataString(queryLocation)}";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                StringLocalizer.Current.Format("WeatherApiCurrentFailed", response.StatusCode),
                null, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<CurrentResponseDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException(StringLocalizer.Current.Get("WeatherApiCurrentInvalid"));
    }

    public async Task<ForecastResponseDto> GetForecastAsync(string language = "en", string? location = null)
    {
        var queryLocation = string.IsNullOrWhiteSpace(location) ? _options.DefaultLocation : location;
        var url = $"forecast.json?key={Uri.EscapeDataString(_options.ApiKey)}&q={Uri.EscapeDataString(queryLocation)}&days={_options.ForecastDays}";
        if (language == "ru") url += "&lang=ru";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                StringLocalizer.Current.Format("WeatherApiForecastFailed", response.StatusCode),
                null, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<ForecastResponseDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException(StringLocalizer.Current.Get("WeatherApiForecastInvalid"));
    }
}
