using System.Globalization;
using System.Text.Json;
using Weather.Localization;

namespace AvaWeather.Services;

public sealed class GeoJsLocationClient(HttpClient client) : IIpLocationClient
{
    public async Task<IpLocation> LocateAsync(CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync("v1/ip/geo.json", cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException(StringLocalizer.Current.Get("IpLocationInvalid"));
        var city = Text(root, "city");
        var region = Text(root, "region");
        var country = Text(root, "country");
        var latitude = Coordinate(root, "latitude");
        var longitude = Coordinate(root, "longitude");
        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(country) ||
            latitude is null or < -90 or > 90 || longitude is null or < -180 or > 180 ||
            (latitude == 0 && longitude == 0))
            throw new InvalidDataException(StringLocalizer.Current.Get("IpLocationIncomplete"));

        return new IpLocation(city, region ?? string.Empty, country, latitude.Value, longitude.Value);
    }

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static double? Coordinate(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.String when double.TryParse(value.GetString(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var coordinate) => coordinate,
            _ => null
        };
    }
}
