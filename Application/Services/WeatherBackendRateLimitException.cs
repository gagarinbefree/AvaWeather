using System.Net;
using Localization;

namespace Application.Services;

/// <summary>WeatherBackendRateLimitException. Stop provider fallback when the shared backend rejects more requests.</summary>
public sealed class WeatherBackendRateLimitException() : HttpRequestException(
    StringLocalizer.Current.Get("WeatherBackendRateLimited"), null, HttpStatusCode.TooManyRequests);
