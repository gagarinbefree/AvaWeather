namespace AvaWeather.Services;

public static class WeatherApiKeyResolver
{
    public static string Resolve(string? environmentValue, string? settingsValue, Func<string?> embeddedKey)
    {
        if (!string.IsNullOrWhiteSpace(environmentValue)) return environmentValue;
        if (!string.IsNullOrWhiteSpace(settingsValue)) return settingsValue;
        return embeddedKey() ?? string.Empty;
    }
}
