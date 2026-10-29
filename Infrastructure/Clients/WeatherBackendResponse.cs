using System.Globalization;
using System.Net;
using Application.Services;
using Localization;

namespace Infrastructure.Clients;

/// <summary>WeatherBackendResponse. Read shared backend limits and retained response dates.</summary>
public static class WeatherBackendResponse
{
    /// <summary>EnsureSuccess. Distinguish shared request limits from a single provider failure.</summary>
    /// <param name="response">The response returned by the weather backend.</param>
    public static void EnsureSuccess(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests && !response.Headers.Contains("X-Weather-Provider"))
            throw new WeatherBackendRateLimitException();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(StringLocalizer.Current.Format("WeatherApiForecastFailed", response.StatusCode), null, response.StatusCode);
    }

    /// <summary>ReadRetainedAt. Keep the original server timestamp when live weather is unavailable.</summary>
    /// <param name="response">The native provider response with optional cache headers.</param>
    /// <returns>The original retained time or null for fresh responses.</returns>
    public static DateTimeOffset? ReadRetainedAt(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("X-Weather-Cache", out var state) || !state.Contains("stale", StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }
        if (!response.Headers.TryGetValues("X-Weather-Fetched-At", out var values) ||
            !DateTimeOffset.TryParse(values.SingleOrDefault(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var time) ||
            time <= DateTimeOffset.UnixEpoch)
        {
            throw new InvalidDataException(StringLocalizer.Current.Get("WeatherBackendCacheInvalid"));
        }

        return time;
    }

    /// <summary>OldestRetainedAt. Keep the oldest retained portion of a combined response.</summary>
    /// <param name="times">The retained dates of the responses used by the weather cards.</param>
    /// <returns>The oldest retained date or null when all responses are fresh.</returns>
    public static DateTimeOffset? OldestRetainedAt(params DateTimeOffset?[] times) => times.Min();
}
