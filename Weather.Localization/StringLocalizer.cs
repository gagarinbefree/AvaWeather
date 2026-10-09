using System.Globalization;
using System.Resources;

[assembly: NeutralResourcesLanguage("en")]

namespace Weather.Localization;

public sealed class StringLocalizer
{
    private static readonly ResourceManager Strings = new("Weather.Localization.Strings", typeof(StringLocalizer).Assembly);
    private static readonly ResourceManager Conditions = new("Weather.Localization.WeatherConditions", typeof(StringLocalizer).Assembly);
    public CultureInfo Culture { get; }

    private StringLocalizer(CultureInfo culture) => Culture = culture;

    public static StringLocalizer For(CultureInfo culture) => new(culture);
    public static StringLocalizer Current => For(CultureInfo.CurrentUICulture);

    public string Get(string key) => Strings.GetString(key, Culture)
        ?? throw new MissingManifestResourceException(string.Format(
            Culture, Strings.GetString("MissingTranslation", Culture)!, key, Culture.Name));

    public string Format(string key, params object[] args) => string.Format(Culture, Get(key), args);

    public string GetOrDefault(string key, string fallback) => Strings.GetString(key, Culture) ?? fallback;

    public string WeatherCondition(int code) => Conditions.GetString($"Code{code}", Culture)
        ?? Conditions.GetString("Unknown", Culture)
        ?? throw new MissingManifestResourceException(Get("MissingWeatherDescriptions"));
}
