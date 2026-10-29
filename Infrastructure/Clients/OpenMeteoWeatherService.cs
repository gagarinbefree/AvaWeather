using System.Globalization;
using System.Text.Json;
using Application.Localization;
using Application.Interfaces;
using Domain.Entities;
using Localization;
using System.Diagnostics;

namespace Infrastructure.Clients;

public sealed class OpenMeteoWeatherService(
    HttpClient client, IPlaceNameLocalizer placeNames, GeoJsLocationClient? ipLocation = null,
    IApiAccessLog? log = null) : IFallbackWeatherService
{
    private const string CurrentVariables =
        "temperature_2m,relative_humidity_2m,apparent_temperature,is_day,precipitation,weather_code,cloud_cover,pressure_msl,wind_speed_10m,wind_direction_10m,visibility,uv_index";
    private const string HourlyVariables =
        "temperature_2m,relative_humidity_2m,apparent_temperature,precipitation_probability,precipitation,weather_code,wind_speed_10m,wind_direction_10m,uv_index,is_day";
    private const string DailyVariables =
        "weather_code,temperature_2m_max,temperature_2m_min,temperature_2m_mean,precipitation_sum,precipitation_probability_max,wind_speed_10m_max,relative_humidity_2m_mean,uv_index_max,sunrise,sunset";

    public async Task<WeatherData> GetWeatherAsync(IpLocation location, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location.City) && ipLocation is not null)
        {
            try
            {
                var ip = await ipLocation.LocateAsync(cancellationToken);
                if (DistanceKm(location.Latitude, location.Longitude, ip.Latitude, ip.Longitude) < 30)
                    location = location with { City = ip.City, Region = ip.Region, Country = ip.Country };
            }
            catch (Exception error) when (!cancellationToken.IsCancellationRequested &&
                error is HttpRequestException or OperationCanceledException or JsonException or InvalidDataException)
            { }
        }
        var url = FormattableString.Invariant(
            $"api/open-meteo/v1/forecast?latitude={location.Latitude}&longitude={location.Longitude}&timezone=auto&forecast_days=3&current={CurrentVariables}&hourly={HourlyVariables}&daily={DailyVariables}");
        var timer = Stopwatch.StartNew();
        log?.Write(new ApiAccessEvent("OpenMeteo", "forecast", "Start", 0, LocationMode: "coordinates"));
        var backend = await RequestAsync(url, timer, log, cancellationToken);
        using var document = backend.Document;

        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException(StringLocalizer.Current.Get("OpenMeteoResponseInvalid"));
        var current = RequiredObject(root, "current");
        var hourly = RequiredObject(root, "hourly");
        var daily = RequiredObject(root, "daily");
        var now = ParseTime(RequiredString(current, "time"));
        var culture = WeatherLanguage.ForCountry(location.Country);
        var currentCode = RequiredInt(current, "weather_code");
        var hourlyTimes = RequiredArray(hourly, "time");
        var dailyTimes = RequiredArray(daily, "time");
        if (hourlyTimes.GetArrayLength() == 0 || dailyTimes.GetArrayLength() < 3)
            throw new InvalidDataException(StringLocalizer.Current.Get("OpenMeteoForecastIncomplete"));

        var result = CreateWeather(location, root, current, hourly, daily, now, culture, currentCode, hourlyTimes, dailyTimes);
        result.ServerCachedAtUtc = backend.RetainedAt;
        if (WeatherLanguage.ApiLanguage(culture) == "ru")
        {
            var place = await placeNames.ResolveRussianAsync(location.City, location.Country,
                location.Latitude, location.Longitude, cancellationToken);
            if (place is not null)
            {
                result.Name = place.City;
                if (!string.IsNullOrWhiteSpace(place.Region)) result.Region = place.Region;
                else if (result.Region.Equals(location.City, StringComparison.OrdinalIgnoreCase))
                    result.Region = place.City;
            }
        }
        return result;
    }

    private async Task<(JsonDocument Document, DateTimeOffset? RetainedAt)> RequestAsync(string url, Stopwatch timer, IApiAccessLog? log,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                log?.Write(new ApiAccessEvent("OpenMeteo", "forecast", "HttpError", timer.ElapsedMilliseconds,
                    (int)response.StatusCode, LocationMode: "coordinates"));
                WeatherBackendResponse.EnsureSuccess(response);
            }
            var retainedAt = WeatherBackendResponse.ReadRetainedAt(response);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            log?.Write(new ApiAccessEvent("OpenMeteo", "forecast", "Success", timer.ElapsedMilliseconds,
                (int)response.StatusCode, LocationMode: "coordinates"));

            return (document, retainedAt);
        }
        catch (HttpRequestException error) when (error.StatusCode is null)
        {
            log?.Write(new ApiAccessEvent("OpenMeteo", "forecast", "TransportError", timer.ElapsedMilliseconds,
                ErrorType: error.InnerException?.GetType().Name ?? error.GetType().Name,
                NetworkError: error.HttpRequestError.ToString(), LocationMode: "coordinates"));
            throw;
        }
        catch (OperationCanceledException error)
        {
            log?.Write(new ApiAccessEvent("OpenMeteo", "forecast", cancellationToken.IsCancellationRequested ? "Canceled" : "Timeout",
                timer.ElapsedMilliseconds, ErrorType: error.GetType().Name, LocationMode: "coordinates"));
            throw;
        }
        catch (JsonException error)
        {
            log?.Write(new ApiAccessEvent("OpenMeteo", "forecast", "InvalidJson", timer.ElapsedMilliseconds,
                ErrorType: error.GetType().Name, LocationMode: "coordinates"));
            throw;
        }
    }

    private static WeatherData CreateWeather(IpLocation location, JsonElement root, JsonElement current,
        JsonElement hourly, JsonElement daily, DateTime now, CultureInfo culture, int currentCode,
        JsonElement hourlyTimes, JsonElement dailyTimes)
    {
        var result = new WeatherData
        {
            Name = string.IsNullOrWhiteSpace(location.City)
                ? FormattableString.Invariant($"{location.Latitude:F2}, {location.Longitude:F2}") : location.City,
            Region = location.Region,
            Country = location.Country,
            Lat = location.Latitude,
            Lon = location.Longitude,
            TzId = OptionalString(root, "timezone") ?? string.Empty,
            LocalTime = now,
            Current = new CurrentWeather
            {
                LastUpdated = now,
                TempC = RequiredDouble(current, "temperature_2m"),
                FeelslikeC = Number(current, "apparent_temperature"),
                Humidity = (int)Number(current, "relative_humidity_2m"),
                ConditionCode = currentCode,
                ConditionText = StringLocalizer.For(culture).WeatherCondition(currentCode),
                WindKph = Number(current, "wind_speed_10m"),
                WindDir = WindDirection(Number(current, "wind_direction_10m")),
                PressureMb = Number(current, "pressure_msl"),
                PrecipMm = Number(current, "precipitation"),
                Cloud = (int)Number(current, "cloud_cover"),
                Uv = Number(current, "uv_index"),
                IsDay = (int)Number(current, "is_day"),
                VisKm = Number(current, "visibility") / 1000
            }
        };

        var today = now.Date;
        var tomorrow = today.AddDays(1);
        for (var i = 0; i < hourlyTimes.GetArrayLength(); i++)
        {
            var time = ParseTime(hourlyTimes[i].GetString() ?? string.Empty);
            if (time <= now)
            {
                if (time.Date == today)
                    result.Current.ChanceOfRain = (int)NumberAt(hourly, "precipitation_probability", i);
                continue;
            }
            if (time.Date != today && time.Date != tomorrow) continue;
            var code = RequiredIntAt(hourly, "weather_code", i);
            result.HourlyForecast.Add(new HourlyForecast
            {
                Time = time,
                TempC = RequiredDoubleAt(hourly, "temperature_2m", i),
                FeelslikeC = NumberAt(hourly, "apparent_temperature", i),
                Humidity = (int)NumberAt(hourly, "relative_humidity_2m", i),
                ConditionCode = code,
                ConditionText = StringLocalizer.For(culture).WeatherCondition(code),
                WindKph = NumberAt(hourly, "wind_speed_10m", i),
                WindDir = WindDirection(NumberAt(hourly, "wind_direction_10m", i)),
                PrecipMm = NumberAt(hourly, "precipitation", i),
                ChanceOfRain = (int)NumberAt(hourly, "precipitation_probability", i),
                IsDay = (int)NumberAt(hourly, "is_day", i),
                Uv = NumberAt(hourly, "uv_index", i)
            });
        }
        if (result.HourlyForecast.Count == 0)
            throw new InvalidDataException(StringLocalizer.Current.Get("OpenMeteoHourlyEmpty"));

        for (var i = 0; i < 3; i++)
        {
            var code = RequiredIntAt(daily, "weather_code", i);
            result.DailyForecast.Add(new DailyForecast
            {
                Date = ParseDate(dailyTimes[i].GetString() ?? string.Empty),
                MaxtempC = RequiredDoubleAt(daily, "temperature_2m_max", i),
                MintempC = RequiredDoubleAt(daily, "temperature_2m_min", i),
                AvgtempC = NumberAt(daily, "temperature_2m_mean", i),
                ConditionCode = code,
                ConditionText = StringLocalizer.For(culture).WeatherCondition(code),
                MaxwindKph = NumberAt(daily, "wind_speed_10m_max", i),
                TotalprecipMm = NumberAt(daily, "precipitation_sum", i),
                Avghumidity = (int)NumberAt(daily, "relative_humidity_2m_mean", i),
                Uv = NumberAt(daily, "uv_index_max", i),
                DailyChanceOfRain = (int)NumberAt(daily, "precipitation_probability_max", i),
                Sunrise = ClockAt(daily, "sunrise", i),
                Sunset = ClockAt(daily, "sunset", i)
            });
        }

        return result;
    }

    private static JsonElement RequiredObject(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object
            ? value : throw new InvalidDataException(StringLocalizer.Current.Format("OpenMeteoFieldMissing", name));

    private static JsonElement RequiredArray(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value : throw new InvalidDataException(StringLocalizer.Current.Format("OpenMeteoFieldMissing", name));

    private static string RequiredString(JsonElement parent, string name) =>
        OptionalString(parent, name) is { Length: > 0 } text
            ? text : throw new InvalidDataException(StringLocalizer.Current.Format("OpenMeteoFieldMissing", name));

    private static string? OptionalString(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static double RequiredDouble(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble() : throw new InvalidDataException(StringLocalizer.Current.Format("OpenMeteoFieldMissing", name));

    private static int RequiredInt(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32() : throw new InvalidDataException(StringLocalizer.Current.Format("OpenMeteoFieldMissing", name));

    private static double Number(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : 0;

    private static double RequiredDoubleAt(JsonElement parent, string name, int index)
    {
        var array = RequiredArray(parent, name);
        return index < array.GetArrayLength() && array[index].ValueKind == JsonValueKind.Number
            ? array[index].GetDouble() : throw new InvalidDataException(StringLocalizer.Current.Format("OpenMeteoIndexedFieldMissing", name, index));
    }

    private static int RequiredIntAt(JsonElement parent, string name, int index)
    {
        var array = RequiredArray(parent, name);
        return index < array.GetArrayLength() && array[index].ValueKind == JsonValueKind.Number
            ? array[index].GetInt32() : throw new InvalidDataException(StringLocalizer.Current.Format("OpenMeteoIndexedFieldMissing", name, index));
    }

    private static double NumberAt(JsonElement parent, string name, int index) =>
        parent.TryGetProperty(name, out var array) && array.ValueKind == JsonValueKind.Array &&
        index < array.GetArrayLength() && array[index].ValueKind == JsonValueKind.Number
            ? array[index].GetDouble() : 0;

    private static string ClockAt(JsonElement parent, string name, int index)
    {
        var array = RequiredArray(parent, name);
        return index < array.GetArrayLength() && array[index].ValueKind == JsonValueKind.String
            ? ParseTime(array[index].GetString() ?? string.Empty).ToString("HH:mm", CultureInfo.InvariantCulture)
            : throw new InvalidDataException(StringLocalizer.Current.Format("OpenMeteoIndexedFieldMissing", name, index));
    }

    private static DateTime ParseTime(string value) =>
        DateTime.TryParseExact(value, "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var time) ? time : throw new InvalidDataException(StringLocalizer.Current.Get("OpenMeteoTimeInvalid"));

    private static DateTime ParseDate(string value) =>
        DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date) ? date : throw new InvalidDataException(StringLocalizer.Current.Get("OpenMeteoDateInvalid"));

    private static string WindDirection(double degrees)
    {
        string[] directions = ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
            "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];
        return directions[(int)Math.Floor((degrees % 360 + 360 + 11.25) % 360 / 22.5)];
    }

    private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double radians = Math.PI / 180;
        var latDelta = (lat2 - lat1) * radians;
        var lonDelta = (lon2 - lon1) * radians;
        var a = Math.Pow(Math.Sin(latDelta / 2), 2) + Math.Cos(lat1 * radians) *
            Math.Cos(lat2 * radians) * Math.Pow(Math.Sin(lonDelta / 2), 2);
        return 6371 * 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }
}
