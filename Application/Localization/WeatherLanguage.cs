using System.Globalization;
using Weather.Localization;

namespace Application.Localization;

public static class WeatherLanguage
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    private static readonly Dictionary<string, string> CountryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Russia"] = "CountryRussia", ["Russian Federation"] = "CountryRussia",
        ["Ukraine"] = "CountryUkraine",
        ["Belarus"] = "CountryBelarus",
        ["Kazakhstan"] = "CountryKazakhstan",
        ["Kyrgyzstan"] = "CountryKyrgyzstan", ["Uzbekistan"] = "CountryUzbekistan",
        ["Tajikistan"] = "CountryTajikistan", ["Turkmenistan"] = "CountryTurkmenistan",
        ["Moldova"] = "CountryMoldova", ["Armenia"] = "CountryArmenia",
        ["Azerbaijan"] = "CountryAzerbaijan", ["Georgia"] = "CountryGeorgia",
        ["Estonia"] = "CountryEstonia", ["Latvia"] = "CountryLatvia", ["Lithuania"] = "CountryLithuania"
    };

    private static string? CountryKey(string? country)
    {
        if (string.IsNullOrWhiteSpace(country)) return null;
        if (CountryKeys.TryGetValue(country.Trim(), out var key)) return key;
        var translations = StringLocalizer.For(Russian);
        return CountryKeys.Values.Distinct().FirstOrDefault(candidate =>
            string.Equals(translations.Get(candidate), country.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static CultureInfo ForCountry(string? country) =>
        CountryKey(country) is not null ? Russian : English;

    public static string DisplayCountry(string country, CultureInfo culture) =>
        CountryKey(country) is { } key ? StringLocalizer.For(culture).Get(key) : country;

    public static string ApiLanguage(CultureInfo culture) =>
        culture.TwoLetterISOLanguageName;
}
