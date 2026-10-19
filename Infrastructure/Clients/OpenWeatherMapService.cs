using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Application.Interfaces;
using Application.Localization;
using Domain.Entities;
using Infrastructure.Configuration;

namespace Infrastructure.Clients;

public sealed class OpenWeatherMapService(HttpClient client, WeatherApiOptions options, IApiAccessLog? log = null,
    IPlaceNameLocalizer? placeNames = null) : IFallbackWeatherService
{
    public async Task<WeatherData> GetWeatherAsync(IpLocation location, CancellationToken cancellationToken = default)
    {
        var language = WeatherLanguage.ApiLanguage(WeatherLanguage.ForCountry(location.Country));
        string Query(string selectedLanguage) => FormattableString.Invariant($"lat={location.Latitude}&lon={location.Longitude}&units=metric&lang={selectedLanguage}&appid={Uri.EscapeDataString(options.OpenWeatherMapApiKey)}");
        using var firstCurrent = await FetchAsync("weather?" + Query(language), "current", cancellationToken);
        if (string.IsNullOrWhiteSpace(location.Country) &&
            firstCurrent.RootElement.TryGetProperty("sys", out var sys) &&
            sys.ValueKind == JsonValueKind.Object &&
            sys.TryGetProperty("country", out var countryCode) && countryCode.ValueKind == JsonValueKind.String)
        {
            try { location = location with { Country = new RegionInfo(countryCode.GetString()!).EnglishName }; }
            catch (ArgumentException) { }
        }
        var resolvedLanguage = WeatherLanguage.ApiLanguage(WeatherLanguage.ForCountry(location.Country));
        using var localizedCurrent = language != resolvedLanguage
            ? await FetchAsync("weather?" + Query(resolvedLanguage), "current", cancellationToken) : null;
        using var forecastDocument = await FetchAsync("forecast?" + Query(resolvedLanguage), "forecast", cancellationToken);
        try
        {
            var result = Map(location, (localizedCurrent ?? firstCurrent).RootElement, forecastDocument.RootElement);
            if (resolvedLanguage == "ru" && placeNames is not null)
            {
                var place = await placeNames.ResolveRussianAsync(result.Name, result.Country,
                    location.Latitude, location.Longitude, cancellationToken);
                if (place is not null)
                {
                    result.Name = place.City;
                    if (!string.IsNullOrWhiteSpace(place.Region)) result.Region = place.Region;
                    else if (result.Region.Equals(location.City, StringComparison.OrdinalIgnoreCase)) result.Region = place.City;
                }
            }
            return result;
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or FormatException or ArgumentOutOfRangeException)
        { throw new InvalidDataException(Localization.StringLocalizer.Current.Get("OpenMeteoResponseInvalid"), ex); }
    }

    private async Task<JsonDocument> FetchAsync(string url, string operation, CancellationToken token)
    {
        var timer = Stopwatch.StartNew();
        log?.Write(new ApiAccessEvent("OpenWeatherMap", operation, "Start", 0, LocationMode: "coordinates"));
        try
        {
            using var response = await client.GetAsync(url, token);
            if (!response.IsSuccessStatusCode)
            {
                log?.Write(new ApiAccessEvent("OpenWeatherMap", operation, "HttpError", timer.ElapsedMilliseconds, (int)response.StatusCode, LocationMode: "coordinates"));
                response.EnsureSuccessStatusCode();
            }
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            var document = await JsonDocument.ParseAsync(stream, cancellationToken: token);
            log?.Write(new ApiAccessEvent("OpenWeatherMap", operation, "Success", timer.ElapsedMilliseconds, (int)response.StatusCode, LocationMode: "coordinates"));
            return document;
        }
        catch (HttpRequestException ex) when (ex.StatusCode is null)
        {
            log?.Write(new ApiAccessEvent("OpenWeatherMap", operation, "TransportError", timer.ElapsedMilliseconds, ErrorType: ex.InnerException?.GetType().Name ?? ex.GetType().Name, NetworkError: ex.HttpRequestError.ToString(), LocationMode: "coordinates"));
            throw;
        }
        catch (OperationCanceledException ex)
        {
            log?.Write(new ApiAccessEvent("OpenWeatherMap", operation, token.IsCancellationRequested ? "Canceled" : "Timeout", timer.ElapsedMilliseconds, ErrorType: ex.GetType().Name, LocationMode: "coordinates"));
            throw;
        }
    }

    private static WeatherData Map(IpLocation location, JsonElement current, JsonElement forecast)
    {
        var offset = TimeSpan.FromSeconds(current.GetProperty("timezone").GetInt32());
        DateTime Local(long epoch) => DateTimeOffset.FromUnixTimeSeconds(epoch).ToOffset(offset).DateTime;
        var now = Local(current.GetProperty("dt").GetInt64());
        var main = current.GetProperty("main");
        var condition = current.GetProperty("weather")[0];
        var wind = current.GetProperty("wind");
        var sys = current.GetProperty("sys");
        var sunrise = Local(sys.GetProperty("sunrise").GetInt64());
        var sunset = Local(sys.GetProperty("sunset").GetInt64());
        var result = new WeatherData
        {
            Name = string.IsNullOrWhiteSpace(location.City) ? current.GetProperty("name").GetString() ?? "" : location.City,
            Region = location.Region, Country = location.Country, Lat = location.Latitude, Lon = location.Longitude, LocalTime = now,
            Current = new CurrentWeather
            {
                LastUpdated = now, TempC = main.GetProperty("temp").GetDouble(), FeelslikeC = main.GetProperty("feels_like").GetDouble(),
                Humidity = main.GetProperty("humidity").GetInt32(), PressureMb = main.GetProperty("pressure").GetDouble(),
                WindKph = Number(wind, "speed") * 3.6, WindDir = Direction(Number(wind, "deg")),
                VisKm = Number(current, "visibility") / 1000, Uv = null,
                ConditionCode = condition.GetProperty("id").GetInt32(), ConditionText = condition.GetProperty("description").GetString() ?? "",
                IsDay = now >= sunrise && now < sunset ? 1 : 0
            }
        };
        var hours = new List<HourlyForecast>();
        foreach (var item in forecast.GetProperty("list").EnumerateArray())
        {
            var time = Local(item.GetProperty("dt").GetInt64());
            var itemMain = item.GetProperty("main");
            var weather = item.GetProperty("weather")[0];
            var itemWind = item.GetProperty("wind");
            var icon = weather.GetProperty("icon").GetString();
            hours.Add(new HourlyForecast { Time = time, TempC = itemMain.GetProperty("temp").GetDouble(),
                FeelslikeC = Number(itemMain, "feels_like"), Humidity = (int)Number(itemMain, "humidity"),
                ConditionCode = weather.GetProperty("id").GetInt32(), ConditionText = weather.GetProperty("description").GetString() ?? "",
                IsDay = icon?.EndsWith('d') == true ? 1 : 0, WindKph = Number(itemWind, "speed") * 3.6,
                WindDir = Direction(Number(itemWind, "deg")), ChanceOfRain = (int)Math.Round(Number(item, "pop") * 100) });
        }
        result.HourlyForecast = hours.Where(h => h.Time > now && h.Time.Date <= now.Date.AddDays(1)).ToList();
        if (result.HourlyForecast.Count == 0) throw new InvalidDataException(Localization.StringLocalizer.Current.Get("OpenMeteoHourlyEmpty"));
        for (var i = 0; i < 3; i++)
        {
            var date = now.Date.AddDays(i);
            var day = hours.Where(h => h.Time.Date == date).ToList();
            if (i == 0) day.Insert(0, new HourlyForecast { Time = now, TempC = result.Current!.TempC,
                Humidity = result.Current.Humidity, WindKph = result.Current.WindKph,
                ConditionCode = result.Current.ConditionCode, ConditionText = result.Current.ConditionText });
            if (day.Count == 0) throw new InvalidDataException(Localization.StringLocalizer.Current.Get("OpenMeteoForecastIncomplete"));
            var representative = day.MinBy(h => Math.Abs(h.Time.Hour - 12))!;
            result.DailyForecast.Add(new DailyForecast { Date = date, MaxtempC = day.Max(h => h.TempC), MintempC = day.Min(h => h.TempC),
                AvgtempC = day.Average(h => h.TempC), ConditionCode = representative.ConditionCode, ConditionText = representative.ConditionText,
                Avghumidity = (int)Math.Round(day.Average(h => h.Humidity)), MaxwindKph = day.Max(h => h.WindKph),
                DailyChanceOfRain = day.Max(h => h.ChanceOfRain), Uv = null,
                Sunrise = i == 0 ? sunrise.ToString("HH:mm", CultureInfo.InvariantCulture) : "—",
                Sunset = i == 0 ? sunset.ToString("HH:mm", CultureInfo.InvariantCulture) : "—" });
        }
        return result;
    }

    private static double Number(JsonElement element, string name) => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : 0;
    private static string Direction(double degrees)
    {
        string[] directions = ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];
        return directions[(int)Math.Floor((degrees % 360 + 360 + 11.25) % 360 / 22.5)];
    }
}
