using System.Globalization;
using System.Diagnostics;
using System.Text.Json;
using Application.Interfaces;
using Weather.Localization;

namespace AvaWeather.Services;

public sealed class GeoJsLocationClient(HttpClient client, IApiAccessLog? log = null) : IIpLocationClient
{
    public async Task<IpLocation> LocateAsync(CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        log?.Write(new ApiAccessEvent("GeoJS", "location", "Start", 0));
        try
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

            log?.Write(new ApiAccessEvent("GeoJS", "location", "Success", timer.ElapsedMilliseconds,
                (int)response.StatusCode));
            return new IpLocation(city, region ?? string.Empty, country, latitude.Value, longitude.Value);
        }
        catch (HttpRequestException error)
        {
            log?.Write(new ApiAccessEvent("GeoJS", "location",
                error.StatusCode is null ? "TransportError" : "HttpError", timer.ElapsedMilliseconds,
                error.StatusCode is null ? null : (int)error.StatusCode.Value,
                ErrorType: error.InnerException?.GetType().Name ?? error.GetType().Name,
                NetworkError: error.StatusCode is null ? error.HttpRequestError.ToString() : null));
            throw;
        }
        catch (OperationCanceledException error)
        {
            log?.Write(new ApiAccessEvent("GeoJS", "location",
                cancellationToken.IsCancellationRequested ? "Canceled" : "Timeout",
                timer.ElapsedMilliseconds, ErrorType: error.GetType().Name));
            throw;
        }
        catch (Exception error) when (error is JsonException or InvalidDataException)
        {
            log?.Write(new ApiAccessEvent("GeoJS", "location", "InvalidResponse",
                timer.ElapsedMilliseconds, ErrorType: error.GetType().Name));
            throw;
        }
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
