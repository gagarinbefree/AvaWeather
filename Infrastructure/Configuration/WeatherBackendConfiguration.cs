using System.Text.Json;
using Localization;

namespace Infrastructure.Configuration;

/// <summary>WeatherBackendConfiguration. Read the public backend address shared by every platform.</summary>
public static class WeatherBackendConfiguration
{
    public static string BaseUrl { get; } = LoadBaseUrl();

    /// <summary>LoadBaseUrl. Read the embedded shared configuration without provider credentials.</summary>
    /// <returns>The backend root used by the native provider routes.</returns>
    private static string LoadBaseUrl()
    {
        using var stream = typeof(WeatherBackendConfiguration).Assembly.GetManifestResourceStream("Infrastructure.Configuration.weather-server.json")
            ?? throw new InvalidDataException(StringLocalizer.Current.Get("WeatherBackendConfigurationInvalid"));
        using var document = JsonDocument.Parse(stream);
        var value = document.RootElement.GetProperty("baseUrl").GetString();
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            uri.UserInfo.Length != 0 || uri.AbsolutePath != "/" || uri.Query.Length != 0 || uri.Fragment.Length != 0)
        {
            throw new InvalidDataException(StringLocalizer.Current.Get("WeatherBackendConfigurationInvalid"));
        }

        return uri.AbsoluteUri;
    }
}
