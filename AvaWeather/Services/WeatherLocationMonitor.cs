using Domain.Entities;

namespace AvaWeather.Services;

// Shares the last successful location between normal weather refreshes and quick location checks.
public sealed class WeatherLocationMonitor(IIpLocationClient locations, IWeatherRepository weather)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IpLocation? currentLocation;

    public async Task<WeatherData> LoadAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            IpLocation? detected;
            try { detected = await locations.LocateAsync(cancellationToken); }
            catch (Exception error) when (IsTransientLocationFailure(error, cancellationToken))
            {
                detected = null;
            }

            var result = detected is null
                ? await weather.GetWeatherAsync(cancellationToken)
                : await weather.GetWeatherAsync(detected, cancellationToken);
            // A failed weather request must not advance the baseline.
            if (detected is not null) currentLocation = detected;
            return result;
        }
        finally { gate.Release(); }
    }

    public async Task<WeatherData?> CheckAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var detected = await locations.LocateAsync(cancellationToken);
            if (SamePlace(currentLocation, detected)) return null;
            var result = await weather.GetWeatherAsync(detected, cancellationToken);
            currentLocation = detected;
            return result;
        }
        finally { gate.Release(); }
    }

    private static bool SamePlace(IpLocation? first, IpLocation second) =>
        first is not null &&
        string.Equals(first.City, second.City, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(first.Country, second.Country, StringComparison.OrdinalIgnoreCase) &&
        Math.Abs(first.Latitude - second.Latitude) < 0.1 &&
        Math.Abs(first.Longitude - second.Longitude) < 0.1;

    private static bool IsTransientLocationFailure(Exception error, CancellationToken cancellationToken) =>
        error is HttpRequestException or System.Text.Json.JsonException or InvalidDataException ||
        error is OperationCanceledException && !cancellationToken.IsCancellationRequested;
}
