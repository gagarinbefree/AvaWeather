using System.Diagnostics;
using System.Text.Json;
using Application.Dtos;
using Application.Interfaces;
using Infrastructure.Configuration;
using Weather.Localization;

namespace Infrastructure.Clients;

public class WeatherApiClient
    (HttpClient httpClient, WeatherApiOptions options, IApiAccessLog? log = null,
        IHttpClientFactory? clientFactory = null) : IWeatherApiClient
{
    public Task<CurrentResponseDto> GetCurrentWeatherAsync(string? location = null)
    {
        var queryLocation = string.IsNullOrWhiteSpace(location) ? options.DefaultLocation : location;
        var url = $"current.json?key={Uri.EscapeDataString(options.ApiKey)}&q={Uri.EscapeDataString(queryLocation)}";
        return RequestAsync<CurrentResponseDto>(url, "current", queryLocation,
            "WeatherApiCurrentFailed", "WeatherApiCurrentInvalid");
    }

    public Task<ForecastResponseDto> GetForecastAsync(string language = "en", string? location = null)
    {
        var queryLocation = string.IsNullOrWhiteSpace(location) ? options.DefaultLocation : location;
        var url = $"forecast.json?key={Uri.EscapeDataString(options.ApiKey)}&q={Uri.EscapeDataString(queryLocation)}&days={options.ForecastDays}";
        if (language == "ru") url += "&lang=ru";
        return RequestAsync<ForecastResponseDto>(url, "forecast", queryLocation,
            "WeatherApiForecastFailed", "WeatherApiForecastInvalid");
    }

    private async Task<T> RequestAsync<T>(string url, string operation, string queryLocation,
        string failureKey, string invalidKey)
    {
        try
        {
            return await RequestOnceAsync<T>(httpClient, "system", url, operation, queryLocation,
                failureKey, invalidKey);
        }
        catch (OperationCanceledException)
        {
            // The system route may recover on the next request; a direct connection may be blocked.
            try
            {
                return await RequestOnceAsync<T>(httpClient, "system", url, operation, queryLocation,
                    failureKey, invalidKey);
            }
            catch (Exception error) when (options.UseDirectConnectionFallback &&
                                          clientFactory is not null && IsConnectionFailure(error))
            {
                return await RequestDirectAsync<T>(url, operation, queryLocation, failureKey, invalidKey);
            }
        }
        catch (HttpRequestException error) when (error.StatusCode is null &&
                                                 options.UseDirectConnectionFallback &&
                                                 clientFactory is not null)
        {
            return await RequestDirectAsync<T>(url, operation, queryLocation, failureKey, invalidKey);
        }
    }

    private async Task<T> RequestDirectAsync<T>(string url, string operation, string queryLocation,
        string failureKey, string invalidKey)
    {
        using var directClient = clientFactory!.CreateClient("WeatherApiDirect");
        return await RequestOnceAsync<T>(directClient, "direct", url, operation, queryLocation,
            failureKey, invalidKey);
    }

    private static bool IsConnectionFailure(Exception error) =>
        error is OperationCanceledException or HttpRequestException { StatusCode: null };

    private async Task<T> RequestOnceAsync<T>(HttpClient client, string route,
        string url, string operation, string queryLocation,
        string failureKey, string invalidKey)
    {
        var mode = queryLocation.Equals("auto:ip", StringComparison.OrdinalIgnoreCase)
            ? "auto:ip" : queryLocation.Contains(',') ? "coordinates" : "configured";
        var timer = Stopwatch.StartNew();
        log?.Write(new ApiAccessEvent("WeatherAPI", operation, "Start", 0,
            LocationMode: mode, ConnectionRoute: route));
        try
        {
            using var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                log?.Write(new ApiAccessEvent("WeatherAPI", operation, "HttpError",
                    timer.ElapsedMilliseconds, (int)response.StatusCode,
                    LocationMode: mode, ConnectionRoute: route));
                throw new HttpRequestException(
                    StringLocalizer.Current.Format(failureKey, response.StatusCode),
                    null, response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = (T?)JsonSerializer.Deserialize(json,
                WeatherApiJsonContext.Default.GetTypeInfo(typeof(T))!);
            if (result is null) throw new InvalidOperationException(StringLocalizer.Current.Get(invalidKey));
            log?.Write(new ApiAccessEvent("WeatherAPI", operation, "Success",
                timer.ElapsedMilliseconds, (int)response.StatusCode,
                LocationMode: mode, ConnectionRoute: route));
            return result;
        }
        catch (HttpRequestException error) when (error.StatusCode is null)
        {
            log?.Write(new ApiAccessEvent("WeatherAPI", operation, "TransportError",
                timer.ElapsedMilliseconds, ErrorType: error.InnerException?.GetType().Name ?? error.GetType().Name,
                NetworkError: error.HttpRequestError.ToString(), LocationMode: mode,
                ConnectionRoute: route));
            throw;
        }
        catch (OperationCanceledException error)
        {
            log?.Write(new ApiAccessEvent("WeatherAPI", operation, "Timeout",
                timer.ElapsedMilliseconds, ErrorType: error.GetType().Name,
                LocationMode: mode, ConnectionRoute: route));
            throw;
        }
        catch (JsonException error)
        {
            log?.Write(new ApiAccessEvent("WeatherAPI", operation, "InvalidJson",
                timer.ElapsedMilliseconds, ErrorType: error.GetType().Name,
                LocationMode: mode, ConnectionRoute: route));
            throw;
        }
        catch (InvalidOperationException error)
        {
            log?.Write(new ApiAccessEvent("WeatherAPI", operation, "InvalidResponse",
                timer.ElapsedMilliseconds, ErrorType: error.GetType().Name,
                LocationMode: mode, ConnectionRoute: route));
            throw;
        }
    }
}
