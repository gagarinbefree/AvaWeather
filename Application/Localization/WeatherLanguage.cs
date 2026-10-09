using System.Globalization;

namespace Application.Localization;

public static class WeatherLanguage
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    private static readonly Dictionary<string, string> RussianCountryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Russia"] = "Россия",
        ["Russian Federation"] = "Россия",
        ["Россия"] = "Россия",
        ["Ukraine"] = "Украина",
        ["Украина"] = "Украина",
        ["Belarus"] = "Беларусь",
        ["Беларусь"] = "Беларусь",
        ["Kazakhstan"] = "Казахстан",
        ["Казахстан"] = "Казахстан",
        ["Kyrgyzstan"] = "Кыргызстан",
        ["Uzbekistan"] = "Узбекистан",
        ["Tajikistan"] = "Таджикистан",
        ["Turkmenistan"] = "Туркменистан",
        ["Moldova"] = "Молдова",
        ["Armenia"] = "Армения",
        ["Azerbaijan"] = "Азербайджан",
        ["Georgia"] = "Грузия",
        ["Estonia"] = "Эстония",
        ["Latvia"] = "Латвия",
        ["Lithuania"] = "Литва"
    };

    public static CultureInfo ForCountry(string? country) =>
        country is not null && RussianCountryNames.ContainsKey(country.Trim()) ? Russian : English;

    public static string DisplayCountry(string country, CultureInfo culture) =>
        culture.TwoLetterISOLanguageName == "ru" && RussianCountryNames.TryGetValue(country.Trim(), out var translated)
            ? translated : country;

    public static string ApiLanguage(CultureInfo culture) =>
        culture.TwoLetterISOLanguageName == "ru" ? "ru" : "en";
}
