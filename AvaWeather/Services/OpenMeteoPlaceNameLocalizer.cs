using System.Text.Json;

namespace AvaWeather.Services;

public sealed class OpenMeteoPlaceNameLocalizer(HttpClient client) : IPlaceNameLocalizer
{
    public async Task<LocalizedPlace?> ResolveRussianAsync(
        string city, string country, double latitude, double longitude,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(country) ||
            latitude is < -90 or > 90 || longitude is < -180 or > 180 ||
            (latitude == 0 && longitude == 0)) return null;

        try
        {
            var exact = await SearchAsync(city, country, latitude, longitude, 10, cancellationToken);
            if (exact is not null || city.Length <= 4) return exact;

            // A short prefix can find variant spellings; coordinates select the actual city.
            return await SearchAsync(city[..4], country, latitude, longitude, 100, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private async Task<LocalizedPlace?> SearchAsync(string city, string country, double latitude,
        double longitude, int count, CancellationToken cancellationToken)
    {
        var query = Uri.EscapeDataString($"{city},{country}");
        using var response = await client.GetAsync(
            $"v1/search?name={query}&count={count}&language=ru", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array) return null;

        LocalizedPlace? closest = null;
        var closestDistance = 30.0;
        foreach (var result in results.EnumerateArray())
        {
            if (!result.TryGetProperty("name", out var nameProperty) ||
                !result.TryGetProperty("latitude", out var latProperty) ||
                !result.TryGetProperty("longitude", out var lonProperty)) continue;

            var name = nameProperty.GetString();
            if (string.IsNullOrWhiteSpace(name) || !ContainsCyrillic(name)) continue;

            var distance = DistanceKm(latitude, longitude, latProperty.GetDouble(), lonProperty.GetDouble());
            if (distance >= closestDistance) continue;

            var region = result.TryGetProperty("admin1", out var admin1) ? admin1.GetString() : null;
            closest = new LocalizedPlace(name, region is not null && ContainsCyrillic(region) ? region : null);
            closestDistance = distance;
        }

        return closest;
    }

    private static bool ContainsCyrillic(string value) =>
        value.Any(character => character is >= '\u0400' and <= '\u04FF');

    private static double DistanceKm(double latitude, double longitude, double otherLatitude, double otherLongitude)
    {
        const double radians = Math.PI / 180;
        var latDelta = (otherLatitude - latitude) * radians;
        var lonDelta = (otherLongitude - longitude) * radians;
        var haversine = Math.Pow(Math.Sin(latDelta / 2), 2) +
            Math.Cos(latitude * radians) * Math.Cos(otherLatitude * radians) *
            Math.Pow(Math.Sin(lonDelta / 2), 2);
        return 6371 * 2 * Math.Asin(Math.Min(1, Math.Sqrt(haversine)));
    }
}
