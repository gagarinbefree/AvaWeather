namespace AvaWeather.Services;

public static partial class EmbeddedWeatherTelemetryDsn
{
    public static string? Read()
    {
        string? dsn = null;
        Populate(ref dsn);
        return dsn;
    }

    static partial void Populate(ref string? dsn);
}
