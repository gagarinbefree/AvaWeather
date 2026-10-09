using System.Globalization;
using Weather.Localization;

namespace AvaWeather.Tests;

public class StringLocalizerTests
{
    [Theory]
    [InlineData("ru-RU", "Ощущается как", "Влажность", "Установка виджета AvaWeather")]
    [InlineData("en-US", "Feels like", "Humidity", "Install AvaWeather widget")]
    public void Strings_are_resolved_by_key_for_each_supported_language(
        string culture, string feelsLike, string humidity, string installerTitle)
    {
        var localizer = StringLocalizer.For(CultureInfo.GetCultureInfo(culture));

        Assert.Equal(feelsLike, localizer.Get("WidgetFeelsLike"));
        Assert.Equal(humidity, localizer.Get("Humidity"));
        Assert.Equal(installerTitle, localizer.Get("InstallerTitle"));
    }

    [Theory]
    [InlineData("ru-RU", "Небольшой дождь")]
    [InlineData("en-US", "Slight rain")]
    public void Weather_descriptions_use_the_same_localizer(string culture, string expected)
    {
        Assert.Equal(expected, StringLocalizer.For(CultureInfo.GetCultureInfo(culture)).WeatherCondition(61));
    }

    [Fact]
    public void Missing_key_fails_instead_of_showing_a_key_to_users()
    {
        Assert.Throws<System.Resources.MissingManifestResourceException>(() =>
            StringLocalizer.For(CultureInfo.GetCultureInfo("en-US")).Get("DoesNotExist"));
    }

    [Theory]
    [InlineData("ru-RU", "Ответ Open-Meteo недействителен.", "Проверка целостности установочного пакета не пройдена.")]
    [InlineData("en-US", "Open-Meteo response is invalid.", "Installer payload integrity check failed.")]
    public void Non_ui_messages_are_localized(string culture, string weatherError, string installerError)
    {
        var localizer = StringLocalizer.For(CultureInfo.GetCultureInfo(culture));
        Assert.Equal(weatherError, localizer.Get("OpenMeteoResponseInvalid"));
        Assert.Equal(installerError, localizer.Get("InstallerPayloadIntegrityFailed"));
    }
}
