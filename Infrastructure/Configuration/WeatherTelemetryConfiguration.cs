namespace Infrastructure.Configuration;

/// <summary>The Hawk Sentry-compatible DSN is a public client identifier, also visible in the PWA.</summary>
public static class WeatherTelemetryConfiguration
{
    public static string? Resolve(string? configuredDsn = null) =>
        Environment.GetEnvironmentVariable("HAWK_DSN") switch
    {
        "off" => null,
        { Length: > 0 } value => value,
        _ => !string.IsNullOrWhiteSpace(configuredDsn)
            ? configuredDsn : AvaWeather.Services.EmbeddedWeatherTelemetryDsn.Read()
    };
}
